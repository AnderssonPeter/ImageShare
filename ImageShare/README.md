todo:
[X] Add nuget mcp
[X] Enable dotnet and typescript LSP
[X] Add powershell to docker
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
  [ ] Keep one list of supported image formats in configuration and use it instead of hardcoding it in multiple places
  [ ] Add endpoint to fetch folder
    [ ] BrowserEndpoint must support duplicate images with different formats
    [ ] BrowserEndpoint should not return the file extension
    [ ] BrowserEndpoint should not list files in the root folder
  [ ] Add endpoint to fetch images
    [ ] Rewrite to take thumbprint from query string as a bool value
    [ ] Don't try to convert in the endpoint, instead loop PreferredConvertFormats and find the first match that the client accepts, if no match is found, return 406 Not Acceptable
    [ ] Use IContentTypeProvider instead of FileExtensionContentTypeProvider and take it as a dependency instead of constructing your own, use the extensionmethod IContentTypeProvider.GetContentType to simplify getting mime type
    [ ] Use PhysicalFileProvider in all endpoints
    [ ] Write missing unit tests for ImageEndpoints
  [X] Find a way to generate thumbprints
  [ ] Move the Paginate method to a helper method and make it generic
  [ ] Make service generate the image in all possible formats
    [ ] Create a mock of IFileProvider that keeps state in memory and use for unit testing
    [ ] There is a mix of Thumbprint and Thumbnail in the codebase, we should standardize on one of them
      [ ] Modify the ThumbnailService to instead convert between formats and also specify a target resolution
      [ ] Rename the folder and all classes inside to something more appropriate, like ImageConversion, ImageConveter
  [ ] Add static analysis unit test that ensures that all minimal endpoints parameters has [FromQuery], [FromRoute], [FromBody], [FromHeader] or [FromServices] attributes
[ ] FE
  [ ] Client Generation
  [ ] editorconfig
  [ ] Add linting
  [ ] Add UI
  [ ] Add to dockerfile
