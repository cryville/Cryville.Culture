[中文](README_zh.md)

# Cryville.Culture
**This project is currently under development, NOT ready for production yet.**

Project #A016 [ccl] Cryville.Culture is a library under .NET that handles culture information. It parses Unicode Common Locale Data from the [Unicode Common Locale Data Repository (CLDR)](https://cldr.unicode.org/) and provides methods that utilize them, including implementations of some algorithms described in the [Unicode Locale Data Markup Language (LDML) specification](https://unicode.org/reports/tr35/).

## Supported data and feature
### Core
- Unicode language and locale identifiers
  - [x] Unicode language identifier
    - [x] Canonicalization
  - [ ] Unicode locale identifier
  - [ ] BCP 47 language tag conversion
  - [x] Validity data
- Locale inheritance and matching
  - [x] Likely subtags
  - [x] Language matching
    - [x] Enhanced language matching
