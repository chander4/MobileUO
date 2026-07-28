# Hollowmere AI Development Guide

This document is the authoritative guide for AI agents working on Hollowmere.

If documentation conflicts with assumptions, trust the documentation.
If runtime state conflicts with documentation, trust runtime state.

---

# Project Goal

Hollowmere is a modern Ultima Online client built on Unity.

Goals:

- Mobile-first UX
- Modern architecture
- Clean, maintainable code
- AI-assisted development
- Zero unnecessary technical debt

The project values long-term maintainability over short-term hacks.

---

# Hollowmere Design Manifesto

Every UI decision should satisfy these principles:

1. Preserve the soul of Ultima Online.
2. Modernize interactions, not gameplay.
3. Never obscure the game world unnecessarily.
4. Touch-first, desktop-quality.
5. Consistency over novelty.
6. Every animation communicates state.
7. Performance before visual effects.
8. Accessibility is a feature, not an afterthought.

# Development Workflow

Before modifying any code:

1. Query OpenDeepWiki for the subsystem.
2. Read the relevant architecture documentation.
3. Inspect the live Unity scene using Unity MCP.
4. Summarize:
   - Current architecture
   - Files that will change
   - Potential regressions
5. Only then begin implementation.

Do not rediscover architecture manually if OpenDeepWiki already contains it.

---

# Definition of Done

A task is NOT complete because it compiles.

Every feature must pass:

- Compile validation
- Scene validation
- Runtime validation
- Gameplay validation
- Device validation (when applicable)

---

# Unity Validation Checklist

Before completing any Unity task verify:

✓ No Console errors

✓ No Missing Script components

✓ No MissingReferenceException

✓ No NullReferenceException

✓ No missing serialized references

✓ Required GameObjects enabled

✓ Scene hierarchy intact

✓ Gameplay verified manually

Never trust tool success messages.

Always inspect Unity state directly.

---

# Root Cause Policy

Never work around bugs.

Always:

1. Find the root cause.
2. Fix the root cause.
3. Add validation to prevent regression.

Do not disable systems to hide errors.

Do not leave TODOs instead of fixes.

---

# Stable Systems

These systems are considered production-stable.

Do not refactor without approval.

- Mobile Input V2
- FloatingJoystick
- LeanTouch integration
- ClientRunner movement
- Raw touch input
- Networking
- Mobile Settings

---

# Protected Systems

Require explicit approval before modification.

- ClientRunner movement logic
- Networking
- Packet serialization
- Asset loading pipeline

---

# Existing Architecture

Movement

Touch
↓

LeanTouch
↓

MobileInputController
↓

FloatingJoystick
↓

ClientRunner
↓

Movement

Networking is intentionally independent of the input system.

---

# Project Standards

Prefer:

- Small incremental commits
- Backwards-compatible changes
- Existing architecture
- Reuse over replacement
- Runtime validation
- Self-healing systems where appropriate

Avoid:

- Large rewrites
- Duplicate systems
- Parallel architectures
- Manual Inspector setup when automation is practical

---

# Verification Requirements

Every completed task must include:

1. Files modified
2. Root cause (if fixing bugs)
3. Validation performed
4. Remaining risks
5. Suggested follow-up work

Do not report success without evidence.

---

# Current Milestone

Modern Server Browser

Current focus:

- Favorites
- Search
- Default Server
- Validation

Do not redesign the login scene without approval.

---

# Last Stable Commit

1cac726

---

# AI Development Philosophy

Optimize for:

- Reliability
- Maintainability
- Testability
- Small reviewable changes

Never optimize solely for fewer lines of code.

Code that is easy to understand is preferred over clever code.

When uncertain:

Stop.

Summarize options.

Ask for direction instead of guessing.