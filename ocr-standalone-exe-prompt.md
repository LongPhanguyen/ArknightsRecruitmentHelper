# Request: self-contained standalone executable for friends to test

Want to distribute RecruitmentOcrApp to friends without them needing to
install .NET themselves (they hit the same "Microsoft.WindowsDesktop.App
8.0.0 missing" error I did before installing the runtime manually).

## Ask
Set up a self-contained, single-file publish configuration so the app runs
standalone on any Windows PC with no .NET installation required.

For .NET/WPF apps this is typically done via `dotnet publish` with these
settings (adjust as needed for the actual project setup):

```
dotnet publish RecruitmentOcrApp -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true
```

- `--self-contained true` bundles the .NET runtime into the output, so
  friends don't need to install anything.
- `-p:PublishSingleFile=true` packages it as one .exe instead of a folder
  full of DLLs, much easier to just hand someone a single file.
- `-r win-x64` targets 64-bit Windows specifically (fine for basically all
  modern PCs).

Please:
1. Confirm this publish configuration works for this project (it uses
   Windows OCR APIs — confirm there's nothing about the OCR dependency that
   breaks self-contained/single-file publishing, since some Windows Runtime
   API bindings can be finicky with single-file trimming).
2. If trimming (`PublishTrimmed`) causes issues with the OCR reflection-
   based APIs, leave trimming off rather than risk broken functionality —
   file size is not a priority here, reliability is.
3. Output the resulting .exe location so I know where to find it after
   running the publish command.
4. Let me know if friends will need anything else at all on their end
   (e.g. Windows version minimum, Visual C++ redistributables, an OCR
   language pack still required regardless of runtime bundling) — I want a
   clear "just double-click this file" experience for them if possible, or
   a short list of one-time prerequisites if not.
