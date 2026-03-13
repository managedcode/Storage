# Pull Requests

Pull requests exist to make a change reviewable, not to dump every edit into one branch.

## Good PR Characteristics

- one coherent change
- clear summary of behaviour, risk, and verification
- linked docs updated when the change affects behaviour or architecture
- no unrelated cleanup mixed into the same review

## Review Readiness Checklist

- scope is small enough to understand
- tests prove the intended behaviour
- commands used for verification are listed
- feature docs or ADRs moved with the change when required

## Smells

- a PR that needs a meeting just to explain what changed
- hidden breaking changes
- “cleanup” commits that alter behaviour without saying so
