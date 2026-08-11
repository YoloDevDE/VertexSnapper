# Coding Rules

Countable rules, not virtues. When in doubt, count - do not judge.

# CLEAN CODE AT ALL COST!!!

# READABILITY >>>> EVERYTHING

## Do not build the future

- **YAGNI.** Only what is needed now. No parameter that is always `null`, no option nobody sets, no `virtual` without an
  existing override.
- Future ideas do not go into code. They go into `docs/IDEAS.md` as one line. Nothing is lost, nothing is in the way.

## Do not abstract too early

- **Rule of Three.** Extract on the third caller. Two duplicates are fine.
- **DRY by reason, not by text.** Merge two blocks only if they would always change together for the same reason.
  Coincidentally identical code stays separate.
- No interface until the second implementation exists.
- No base class until the second subclass exists.

## Types

Prefer a bigger class over a new type that exists only to make a class smaller. A type must earn its name by having its
own responsibility.

- **Every type is `public`.** No `internal`, no `private` types.
- **No nested types.** No class inside a class, no enum inside a class, no struct, record, delegate or interface inside
  a class either.
- **One type per file**, and the file is named after it. This holds for enums, interfaces, structs and records exactly
  like for classes.
- Small classes over one large class with many private members.

## Avoid the C#-only corners

Code should read like plain object-oriented code. If a Java 8 developer would not recognize it, it needs a reason.

- **No `partial`.** If a class only fits by being split across files, it is too big - split the responsibility instead.
- **No `ref`, no `out`.** Return a value, or return a type that holds both values. `int.TryParse` and friends from the
  framework are fine to call - just do not write your own.
- **No `unsafe`, no `dynamic`, no `goto`.**
- LINQ is fine, it is just Streams.

## Control flow

- **No `else`. Ever.** Also no `else if`. If two cases exclude each other, the first one returns. If there are many
  cases, use a `switch` or a lookup, not a chain.
- **Guard clauses everywhere.** Handle the invalid case first and return. The happy path stays at the left edge,
  unindented, to the end of the method.
- Do not wrap the body of a method in one big `if`. Invert it and return early.
- **One nesting level per method.** A block inside a block means the inner block becomes its own method. `foreach`
  containing an `if` that only guards: use `continue`.

## Working in this repo

- Match the surrounding code: naming, comment density, formatting.
- Do not touch files the task does not name.
- Chesterton's Fence: understand odd code before deleting it.
- Between two workable approaches, the one introducing fewer new names wins.
- No comment that repeats what the code already says.
- US English everywhere in the repo (color, center, behavior).
