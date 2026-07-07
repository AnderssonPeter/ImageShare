todo:
[X] Add nuget mcp
[X] Enable dotnet and typescript LSP
[X] Add powershell to docker
[ ] Modify the script that starts the open code container, fix the todos in it!
[ ] BE
  [X] Add run script
    [X] Add dotnet format
    [X] Add test start application
      [ ] Modify to scan the output instead of doing a health check
      [ ] Move the script to a file
  [X] NativeAOT
  [X] editorconfig
  [X] Linting
  [X] Scalar
  [X] Add user class
  [X] Add canAccessFolder method to user class
    [X] Move the regex generation logic into it's own class that caches regexes for each filter.
    [X] Add unit tests for canAccessFolder method
  [X] Parse scopes to detect what images we are allowed to read
  [X] Keep one list of supported image formats in configuration and use it instead of hardcoding in both ImageEndpoints and ThumbprintService, it should be it's own options object and in appsettings.json it should be avif, webp, jpg, png.
  [X] When adding options add validation attributes and validate them on startup
  [ ] Use DI in unit tests!?
  [ ] See if we can find a better way to structure the endpoints
  [X] Use typed result sets, and get rid of IsStatusCode helper method!
  [X] ServeBestMatchAsync and ServeImageAsync should not be async!
  [X] Add endpoint to fetch folder
    [X] BrowserEndpoint must support duplicate images with different formats
    [X] BrowserEndpoint should not return the file extension
    [X] BrowserEndpoint should not list files in the root folder
  [ ] Add a function to get a random image in a folder
  [ ] Don't list empty folders in BrowserEndpoint
  [X] Add endpoint to fetch images
    [X] Use IContentTypeProvider instead of FileExtensionContentTypeProvider and take it as a dependency instead of constructing your own, use the extension method IContentTypeProvider.GetContentType to simplify getting mime type
    [X] Rewrite to take thumbprint from query string as a bool value
    [X] Don't try to convert in the endpoint, instead loop PreferredConvertFormats and find the first match that the client accepts, if no match is found, return 406 Not Acceptable
    [X] Write missing unit tests for ImageEndpoints
    [X] To find a matching image check the smallest first then the next and so on
    [X] Do not convert to thumbnail in ImageEndpoints, modify so that it looks for thumbnail files in FindMatchingFiles instead
  [X] Find a way to generate thumbprints
  [ ] Add common instructions
    [X] All options must be validated on startup
    [X] Do not use abbreviations
    [X] ImageConverterJobTests should not use a physical directory and instead use the memory file provider!
    [X] Do not use reflection in tests to access method, instead make it internal and use InternalsVisibleTo attribute to access it in tests
    [X] Do not use time based tests, Task.Delay is not a feasible solution
  [ ] there should be some way to use IContentTypeProvider without constructing it on our own, while adding additional file formats to it?
  [X] Move the Paginate method to a helper method and make it generic and reuse it in both BrowserEndpoint and ImageEndpoints
  [X] Make service generate the image in all possible formats
    [X] There is a mix of Thumbprint and Thumbnail in the codebase, we should standardize on one of them
      [X] Modify the ThumbnailService to instead convert between formats and also specify a target resolution
      [X] Rename the folder and all classes inside to something more appropriate, like ImageConversion, ImageConveter
    [X] Generate thumbnails for all formats and in all formats
    [X] Change from PhysicalFileProvider to WritablePhysicalFileProvider, register it as both IAsyncWritableFileProvider and IFileProvider, use IAsyncWritableFileProvider when creating new files.
    [ ] use an enum for image formats instead of magic strings
  [ ] Add static analysis unit test that ensures that all minimal endpoints parameters has [FromQuery], [FromRoute], [FromBody], [FromHeader] or [FromServices] attributes
  [ ] Fork and add cancellationToken to ReadAsBytesAsync
[ ] FE
  [ ] Client Generation
  [ ] editorconfig
  [ ] Add linting
  [ ] Add UI
  [ ] Add to dockerfile
