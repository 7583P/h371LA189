# Security Policy

The **Hill Adventurer** team takes project security and maintainer safety seriously. This document describes our supported versions, secret handling procedures, and responsible vulnerability disclosure process.

---

## Supported Versions

Only the current active branch and latest tagged release receive security fixes.

| Version | Supported | Notes |
|:---|:---:|:---|
| `1.2.x` (Unity 6000.3.13f1) | :white_check_mark: | Current active release branch |
| `< 1.2.0` | :x: | Legacy / unsupported |

---

## Reporting a Security Vulnerability

If you discover a security vulnerability, sensitive data leakage, or credential exposure:

1. **Do NOT open a public GitHub issue.** Public issues disclose potential vulnerabilities before a mitigation is available.
2. Please report the vulnerability privately via **[GitHub Security Advisories](https://github.com/7583P/h371LA189/security/advisories/new)** or by emailing the project maintainer at `matiasxdfuaqpro910@gmail.com`.
3. In your report, please include:
   - Type of issue (e.g., hardcoded credential, secret leakage, serialization flaw).
   - Affected file path, commit hash, or component.
   - Step-by-step reproduction instructions or proof of concept.
   - Recommended remediation if known.

### Response Timeline
- **Initial Acknowledgement:** Within 48 hours.
- **Triage & Assessment:** Within 5 business days.
- **Resolution & Release:** We will coordinate a remediation patch and credit the reporter upon public release.

---

## Secrets & Credential Management Policy

### Strict Prohibition
The following files and tokens must **never** be committed to this repository:
- Android signing keys and keystores (`*.keystore`, `*.jks`, `*.p12`, `*.pfx`, `*.pem`).
- Environment variable definitions (`.env`, `.env.*`).
- Production Google AdMob Publisher IDs and Ad Unit IDs (use test IDs from `AdMobConfiguration.asset.example`).
- Google Play / Firebase service account keys (`google-services.json`, `*credentials*.json`).
- Personal tokens or private certificates.

### Android Signing Key Rotation Notice
If an Android release signing key was ever exposed in public commit history:
- If enrolled in **Google Play App Signing**: The published key on Google's cloud is secure. The exposed local key functions as an *upload key*. The project maintainer must request an upload key reset directly in the [Google Play Console](https://play.google.com/console) under **Release > Setup > App integrity**.
- If not enrolled in App Signing: The keystore cannot be changed without reissuing the application under a new package identifier or rolling out an APK Signature Scheme v3 key upgrade.
- Local release signing keys should always be configured via local environment variables (`ANDROID_KEYSTORE_PATH`, `ANDROID_KEYSTORE_PASS`) and kept in untracked local paths.

### Automated CI Safeguards
- This repository runs **Gitleaks** secret scanning on every pull request and push to prevent credential leaks.
- Disallowed file patterns (`*.keystore`, `*.jks`, `.env`) are enforced by the CI pipeline before merges.
