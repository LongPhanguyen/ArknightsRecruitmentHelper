# Operator lookup data source: use Aceship's akhr.json (path needs verification)

Next feature: groundwork for showing which specific operators (4★/5★/6★)
can come from a given set of detected recruitment tags — not just "this
combo guarantees 4★+" but "here are the actual operators possible."

## Data source
Planning to use `akhr.json` from the Aceship/AN-EN-Tags GitHub repo — a
community-maintained Arknights data file with English operator names,
rarity, and full tag lists per operator.

**Known issue: the old documented path 404s.**
`https://raw.githubusercontent.com/Aceship/AN-EN-Tags/master/akhr.json` does
not work. I checked the repo directly and it's been restructured — there's
now a `json/` subfolder in the repo root (alongside `css/`, `js/`, `extra/`,
`scripts/`) instead of loose JSON files at the repo root. The file has
likely just been moved into that folder, possibly still named `akhr.json`,
but this needs confirming rather than assuming.

## What to do
1. Browse/fetch `https://github.com/Aceship/AN-EN-Tags/tree/master/json`
   (or query the GitHub API, e.g.
   `https://api.github.com/repos/Aceship/AN-EN-Tags/contents/json`) to find
   the actual current filename.
2. Once found, confirm the correct raw URL works — likely something like
   `https://raw.githubusercontent.com/Aceship/AN-EN-Tags/master/json/akhr.json`,
   but verify the exact path and filename rather than assuming.
3. Fetch the file and check its actual schema. Expected to be roughly:
   ```json
   {
     "name": "OperatorName",
     "level": 5,          // rarity, 1-6
     "type": "Caster",    // class tag
     "tags": ["Caster", "AoE", "Nuker"],  // full tag array, mixed categories
     "sex": "Male",
     "hidden": false        // false = currently in the recruitment pool
   }
   ```
   Confirm/correct this against the real file — don't assume it's exactly
   right.
4. Check the file's last-modified date via the GitHub API or commit history
   for that specific file. Flag clearly if it looks stale (missing recent
   operator releases) before building further on top of it.
5. Add a step (build-time download, or app-startup fetch with local cache —
   your call on which fits the project better) to pull this JSON and store
   it locally so the app doesn't need network access every launch.
6. Deserialize into a C# model: operator name, rarity, and full tag list.
   Filter out entries where `hidden == true` (not in the current recruitment
   pool) for the main lookup — but keep the raw data available in case we
   want to reference non-recruitable operators later.
7. Build the lookup function: given a set of currently detected tags (from
   the OCR capture), return all operators whose tag list is a SUBSET of the
   detected tags — i.e. every tag that operator requires is present among
   what was detected. This is the "which operators are actually possible in
   this recruitment" logic.
8. No UI needed yet — just get this working as a testable function/service
   layer. A simple console/debug output showing "given these detected tags,
   here are the matching operators and their rarities" is enough to confirm
   it works correctly.

## Things to flag back to me
- The confirmed correct path/filename once found.
- How stale or fresh the file's last commit actually is — if it looks
  clearly out of date, say so before we build further on top of it.
- Whether `tags` in the file already separates cleanly into the four
  categories (Class/Position/Qualification/Affix) or if it's just one mixed
  bag per operator — this affects whether we can reuse this same tag list
  for the earlier rarity-combo feature or if that needs separate handling.
