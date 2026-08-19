# Changelog

All notable changes to this project are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and this
project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

Nothing has been released yet. The repository builds as `0.1.0-dev.N` until the first `v` tag.

### Added

- `RibbonItemSize` and `RibbonItemSizes`: the three shapes an item can take, and the set of them an
  item declares it accepts.
- The layout that decides between those shapes, ahead of the control that will use it. Items step
  down through the shapes they accept before any group gives up; the group with the lowest priority
  gives way first and ties are broken from the right; a group that no longer fits folds into a
  button instead of leaving the strip; and the last resort is those buttons dropping their labels,
  one at a time and in the same order. The arrangements are generated without reference to the width
  available, which only chooses which of them to stop at, and that is what keeps the result stable,
  reversible and free of flicker.
