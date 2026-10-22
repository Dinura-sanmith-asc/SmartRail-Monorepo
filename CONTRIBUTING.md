# Contributing

Create a branch from `main` and submit a pull request. Keep application changes within their directory, and include related API and client updates in the same pull request when contracts change.

## Adding an application

Place runnable products under `apps/<name>` and backend services under `services/<name>`. Give each project a README with setup, configuration, build, and deployment instructions. Add it to the root README and CI. Add shared code under `packages` only when at least two projects use the same maintained contract or implementation.

## Migration notes

The initial imports used `git subtree add` without squashing the selected source branch history. Original repositories and worktrees remain the source of any other branches. The local edits present during migration were copied into the monorepo as a follow-up commit. The document handoff had no Git history.

Do not copy the old parent directory wholesale. Review new files for secrets before committing or publishing. Keep generated outputs and machine-specific configuration ignored in their project directories.
