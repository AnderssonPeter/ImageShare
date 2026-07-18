todo:
[X] Add nuget mcp
[X] Enable dotnet and typescript LSP
[X] Add powershell to docker
[ ] Add Auth endpoint where you can provide a filter (same as `User.ImageShareFilter` and a end date, and returns a signed jwt token
  [ ] Add a auth endpoint that accepts the jwt token from above as sign in
  [ ] This should not replace the other auth both should work
  [ ] To create the jwt token an admin role should be required
[ ] Add api key authentication
  [ ] This should not replace the other auth all three should work
  [ ] The api keys should be stored in the settings file, with a `ImageShareFilter`
[X] Modify the script that starts the open code container, fix the todos in it!
[ ] Allow opencode to access tmp folder by default
[X] What is rg cli tool? install in container?
[X] Add nuget mcp server
[X] Enable microsoft docs mcp server
[X] Add instructions to group files by funcitionality not type
[X] Add Arrange, Act, Assert comments to unit tests
[X] Convert unit tests to parameterized unit tests where it makes sense
[X] Add TestUser to DI
[X] Disallow root paths
[ ] Folder endpoint should only return files that have a image file extension
[ ] Add endpoint to download multiple images, from multiple folders recursively
[ ] Add endpoint to get random image from a list of folder recursively
[X] In unit tests move AddDir (rename to AddDirectory), AddFile, AddImageFile, AddThumbFile, Unwrap and other common methods to extension methods
[X] Add CreateThumbnail, IsStatusCode, CreateTestImage, to a base class for unit tests
[X] Move the code for `dotnet r startup` to a powershell file, check if the Redirect logic is needed, if not then remove if it's needed make sure its compatible with both linux and windows
[X] Create a user mock class that can be reused in all unit tests instead of having one per test file
  [X] The mock class should be added to DI and resolved using DI (the tests should not call new on it)
[X] HasVisibleContent
  [X] should not return true when it finds a directory, it has to run recursively on sub directories
  [X] Thumbprints should not be included in the calculation, only images with the correct file extension should be included
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
  [ ] Don't use _ for private fields
    [ ] Configure .editorconfig accordingly
  [X] Add canAccessFolder method to user class
    [X] Move the regex generation logic into it's own class that caches regexes for each filter.
    [X] Add unit tests for canAccessFolder method
  [X] Parse scopes to detect what images we are allowed to read
  [X] Keep one list of supported image formats in configuration and use it instead of hardcoding in both ImageEndpoints and ThumbprintService, it should be it's own options object and in appsettings.json it should be avif, webp, jpg, png.
  [X] When adding options add validation attributes and validate them on startup
  [X] Use DI in unit tests!?
  [ ] See if we can find a better way to structure the endpoints
  [X] Use typed result sets, and get rid of IsStatusCode helper method!
  [X] ServeBestMatchAsync and ServeImageAsync should not be async!
  [X] Add endpoint to fetch folder
    [X] BrowserEndpoint must support duplicate images with different formats
    [X] BrowserEndpoint should not return the file extension
    [X] BrowserEndpoint should not list files in the root folder
  [X] Add a function to get a random thumbnail image in a folder
    [X] Move GetRandomThumbnail from BrowsingEndpoints to ImageEndpoints
    [ ] No unit test should create a new InMemoryFileProvider and instead use the one provided by Dependency Injection
    [ ] Unit tests should use IWritableFileProvider and IFileProvider instead of concrete implementations
    [ ] Convert `/random-thumbnail/{**path}` to get `/random/{**path}` with a parameter to specify if you want a full image or thumbnail, and a parameter if to get recursively
  [X] Don't list empty folders in BrowserEndpoint
  [X] Add endpoint to fetch images
    [X] Use IContentTypeProvider instead of FileExtensionContentTypeProvider and take it as a dependency instead of constructing your own, use the extension method IContentTypeProvider.GetContentType to simplify getting mime type
    [X] Rewrite to take thumbprint from query string as a bool value
    [X] Don't try to convert in the endpoint, instead loop PreferredConvertFormats and find the first match that the client accepts, if no match is found, return 406 Not Acceptable
    [X] Write missing unit tests for ImageEndpoints
    [X] To find a matching image check the smallest first then the next and so on
    [X] Do not convert to thumbnail in ImageEndpoints, modify so that it looks for thumbnail files in FindMatchingFiles instead
  [X] Find a way to generate thumbprints
  [X] Add common instructions
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
  [ ] Add mcp server for tanstack
  [ ] Add mcp server for shadcn
  [ ] Client Generation
  [ ] editorconfig
  [ ] Add linting
  [ ] Add UI
  [ ] Add to dockerfile
