#!/usr/bin/env python3
"""End-to-end smoke test for the running compose stack.

Drives the web app the way a browser does (real Keycloak login, cookies, redirects) and checks one
journey per persona against the dev seed. Standard library only. Usage: scripts/smoke.py [base-url]
"""
import html
import http.cookiejar
import re
import sys
import time
import urllib.error
import urllib.parse
import urllib.request

WEB = (sys.argv[1] if len(sys.argv) > 1 else "http://localhost:3000").rstrip("/")
PASSWORD = "demo-password"
EMILY_ID = "b0000000-0000-0000-0000-000000000001"


class NoRedirect(urllib.request.HTTPRedirectHandler):
    def redirect_request(self, *args, **kwargs):
        return None


class LocalhostPolicy(http.cookiejar.DefaultCookiePolicy):
    # Browsers send Secure cookies to http://localhost; Python's default policy does not.
    def return_ok_secure(self, cookie, request):
        return True


class Browser:
    def __init__(self):
        self.jar = http.cookiejar.CookieJar(policy=LocalhostPolicy())
        self.opener = urllib.request.build_opener(urllib.request.HTTPCookieProcessor(self.jar), NoRedirect)

    def request(self, url, data=None, headers=None):
        try:
            response = self.opener.open(urllib.request.Request(url, data=data, headers=headers or {}), timeout=30)
        except urllib.error.HTTPError as error:
            response = error
        return response.status, {k.lower(): v for k, v in response.headers.items()}, response.read().decode("utf-8", "replace")

    def follow(self, url, data=None, headers=None, limit=10):
        for _ in range(limit):
            status, response_headers, body = self.request(url, data, headers)
            data = None
            location = response_headers.get("location")
            if status in (301, 302, 303, 307, 308) and location:
                url = urllib.parse.urljoin(url, location)
                continue
            return status, url, body
        raise RuntimeError("too many redirects")


def text(body):
    return re.sub(r"\s+", " ", re.sub(r"<[^>]*>", " ", body.replace("<!-- -->", "")))


def server_action(body, containing=None):
    for form in re.findall(r"<form.*?</form>", body, re.S):
        if containing is None or containing in form:
            found = re.search(r'name="(\$ACTION_ID_[a-f0-9]+)"', form)
            if found:
                return found.group(1)
    raise RuntimeError("no server action on the page")


def post_action(browser, url, action):
    boundary = "----smoke"
    body = f'--{boundary}\r\nContent-Disposition: form-data; name="{action}"\r\n\r\n\r\n--{boundary}--\r\n'
    return browser.request(url, body.encode(), {"Content-Type": f"multipart/form-data; boundary={boundary}"})


def sign_in(browser, email):
    _, _, login = browser.follow(f"{WEB}/login")
    status, headers, _ = post_action(browser, f"{WEB}/login", server_action(login))
    _, _, keycloak_page = browser.follow(headers["location"])
    form = re.search(r'<form[^>]*id="kc-form-login"[^>]*action="([^"]+)"', keycloak_page, re.S)
    if not form:
        raise RuntimeError("Keycloak did not show its login form")
    credentials = urllib.parse.urlencode({"username": email, "password": PASSWORD, "credentialId": ""}).encode()
    status, headers, _ = browser.request(
        html.unescape(form.group(1)), credentials, {"Content-Type": "application/x-www-form-urlencoded"}
    )
    if "location" not in headers:
        raise RuntimeError(f"Keycloak refused the login for {email} (status {status})")
    return browser.follow(headers["location"])


def sign_out(browser, page_url):
    _, _, page = browser.follow(page_url)
    _, headers, _ = post_action(browser, page_url, server_action(page))
    return browser.follow(headers["location"])


failures = []


def check(name, ok, detail=""):
    print(f"  {'PASS' if ok else 'FAIL'}  {name}" + (f"  ({detail})" if detail and not ok else ""))
    if not ok:
        failures.append(name)


def wait_for_web(timeout=120):
    deadline = time.time() + timeout
    while time.time() < deadline:
        try:
            if Browser().request(f"{WEB}/login")[0] == 200:
                return
        except OSError:
            pass
        time.sleep(2)
    raise SystemExit(f"The web app did not answer at {WEB}/login within {timeout}s")


wait_for_web()

print("Patient: Emily signs in and sees her results")
emily = Browser()
_, url, page = sign_in(emily, "emily.carter@demo.example")
check("lands on the patient home", url == f"{WEB}/patient", url)
check("sees her seeded lab results", "4 results on record" in text(page), text(page)[:200])
_, _, consents = emily.follow(f"{WEB}/patient/consents")
check("sees the consent she gave Riverside Clinic", "Riverside Clinic can view your hematology and lipids results" in text(consents))

print("Clinician: Dr. Thompson reads Emily's results")
sarah = Browser()
_, url, page = sign_in(sarah, "sarah.thompson@demo.example")
check("lands on the clinician home", url == f"{WEB}/clinician", url)
check("sees Emily as a patient she treats", "Emily Carter" in text(page) and "You treat this patient" in text(page))
_, _, chart = sarah.follow(f"{WEB}/clinician/patients/{EMILY_ID}")
check("sees all four categories", all(c in chart for c in ("biochemistry", "endocrinology", "hematology", "lipids")))
check("is told the access is recorded", "This access is recorded" in text(chart))

print("Patient: Emily can see who read her results")
_, _, history = emily.follow(f"{WEB}/patient/access-history")
check("the read is in her access history", "Dr. Sarah Thompson (Northside Clinic) viewed 4 lab results" in text(history), text(history)[:300])

print("Researcher: Dr. Davies sees consented participants without names")
laura = Browser()
_, url, page = sign_in(laura, "laura.davies@demo.example")
check("lands on the researcher home", url == f"{WEB}/researcher", url)
check("sees a participant", re.search(r"Participant [0-9A-F]{8}", text(page)) is not None)
check("sees no patient names", "James Wilson" not in text(page) and "Emily Carter" not in text(page))

print("Boundaries")
status, url, _ = emily.follow(f"{WEB}/clinician")
check("a patient cannot open the clinician area", url == f"{WEB}/patient", url)
_, url, _ = sign_in(Browser(), "admin@demo.example")
check("a login with no portal record gets no access", url == f"{WEB}/login", url)
_, url, _ = Browser().follow(f"{WEB}/patient")
check("signed-out visitors are sent to sign in", url == f"{WEB}/login", url)

print("Sign out")
_, url, _ = sign_out(emily, f"{WEB}/patient")
check("returns to the sign-in page", url == f"{WEB}/login", url)
_, url, _ = emily.follow(f"{WEB}/patient")
check("the session is gone", url == f"{WEB}/login", url)

if failures:
    print(f"\n{len(failures)} check(s) failed")
    sys.exit(1)
print("\nAll checks passed")
