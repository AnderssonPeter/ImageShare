todo:
[X] Add nuget mcp
[X] Enable dotnet and typescript LSP
[X] Add powershell to docker
[X] Dynamic Context Pruning Plugin https://github.com/Opencode-DCP/opencode-dynamic-context-pruning
[X] Allow opencode to access tmp folder by default
[X] mcp to search code base https://github.com/Helweg/opencode-codebase-index
[X] Fix context7 auth go into container and curl with "" around the url!
[ ] Install headroom
[X] Add Auth endpoint where you can provide a filter (same as `User.ImageShareFilter` and a end date, and returns a signed jwt token
  [X] The endpoint that generates a JWT should verify that the user has a admin role (what the role is named should be configured in appsettings, the admin role can only exist when authing from open id connect)
  [X] Add a auth endpoint that accepts the jwt token from above as sign in
  [X] To create the jwt token an admin role should be required
[X] Add api key authentication
  [X] This should not replace the other auth all three should work
  [X] The api keys should be stored in the settings file, with a `ImageShareFilter`
[X] Do not use magic string, check if they are defined in some other class or create a const, Example "image_share_filter", "name", "display_name"
[X] Modify the script that starts the open code container, fix the todos in it!
[X] What is rg cli tool? install in container?
[X] Add nuget mcp server
[X] Enable microsoft docs mcp server
[X] Add instructions to group files by funcitionality not type
[X] Add Arrange, Act, Assert comments to unit tests
[X] Convert unit tests to parameterized unit tests where it makes sense
[X] Add TestUser to DI
[X] Disallow root paths
[X] Folder endpoint should only return files that have a image file extension
[X] Add endpoint to download multiple images, from multiple folders recursively
[X] Add endpoint to get random image from a list of folder recursively
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
      [X] Modify to scan the output instead of doing a health check
      [X] Move the script to a file
  [X] NativeAOT
  [X] editorconfig
  [X] Linting
  [X] Scalar
  [X] Add user class
  [X] Don't use _ for private fields
    [X] Configure .editorconfig accordingly
  [X] Try not to create helper classes or service classes, create extension methods or classes that handle the logic
    [X] BrowsingHelpers.IsImageFile/IsHiddenFile could be added to a `RelativePath` class, that is converted from string.
    [X] `PathHelper` could also be merged into `RelativePath`
    [X] BrowsingHelpers.HasVisibleContent Should be an extension method on IFileProvider
    [X] Add XML comments to the newly created methods
  [X] Add custom exception mapper
    [X] Use custom exceptions for different types of errors
    [X] Add EnsureAuthenticated on IUser
  [X] Don't check if `IsAuthenticated` inside Command/QueryHandler, use a behavior and attribute for that!
  [X] Merge EnumerateImageFiles and GetImageBaseNames and add parameter for recursive or not
  [X] Do we need FindMatchingFilesRecursive can't we just rewrite FindMatchingFiles to handle it correctly?
  [X] There are multiple requests that don't work, create integration tests.
  [X] /folders without any folders listed dosen't work so its impossible to list the root folders.
  [X] /images/download fails due to some `System.InvalidOperationException: Synchronous operations are disallowed. Call WriteAsync or set AllowSynchronousIO to true instead.` error
  [X] Path parameters are incorrectly sent from scalar, i don't know if its a bug in scalar or a bug in how we specified the path variable in openapi, the path parameter is url encoded so / is replaced by %2f
  [X] When a path is provided that doesn't exist return 404
  [X] Image converter job doesn't seem to work
  [X] When you only download one folder, don't create a folder for it inside the zip
  [X] Add canAccessFolder method to user class
    [X] Move the regex generation logic into it's own class that caches regexes for each filter.
    [X] Add unit tests for canAccessFolder method
  [X] Parse scopes to detect what images we are allowed to read
  [X] Keep one list of supported image formats in configuration and use it instead of hardcoding in both ImageEndpoints and ThumbprintService, it should be it's own options object and in appsettings.json it should be avif, webp, jpg, png.
  [X] When adding options add validation attributes and validate them on startup
  [X] Use DI in unit tests!?
  [X] See if we can find a better way to structure the endpoints
  [X] Use typed result sets, and get rid of IsStatusCode helper method!
  [X] ServeBestMatchAsync and ServeImageAsync should not be async!
  [X] Add endpoint to fetch folder
    [X] BrowserEndpoint must support duplicate images with different formats
    [X] BrowserEndpoint should not return the file extension
    [X] BrowserEndpoint should not list files in the root folder
  [X] Add a function to get a random thumbnail image in a folder
    [X] Move GetRandomThumbnail from BrowsingEndpoints to ImageEndpoints
    [X] No unit test should create a new InMemoryFileProvider and instead use the one provided by Dependency Injection
    [X] Unit tests should use IWritableFileProvider and IFileProvider instead of concrete implementations
    [X] Convert `/random-thumbnail/{**path}` to get `/random/{**path}` with a parameter to specify if you want a full image or thumbnail, and a parameter if to get recursively
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
  [X] Add static analysis unit test that ensures that all minimal endpoints parameters has [FromQuery], [FromRoute], [FromBody], [FromHeader] or [FromServices] attributes
  [X] Call OpenApi endpoint instead of root in startup.ps1
  [ ] Fork WritableFileProvider and add cancellationToken to ReadAsBytesAsync
  [X] Write openapi spec to disk on build
  [X] Include full path in /content and /content/{path}
  [X] Require the user to be logged in to get the frontend!
  [X] Add frontend proxy
  [ ] Chrome MCP server? / Playwright MCP
  [ ] Download log
  [ ] Images aren't being converted when added by rapidraw?
  [X] Create unit tests that tries to forge an jwt token
  [X] Add rate limit to unauthenticated endpoints
  [X] Break up the agent.md into skills to lower the token usage
  [ ] Add a usage agreement function
  [X] Add support for negative glob
  [X] Verify that the random function wont return images for folders where the user dosen't have access
  [ ] Sort images by created date, set created date from metadata
  [X] Microsoft.AspNetCore.Hosting.Diagnostics[11]
      Hosting startup assembly exception
      System.InvalidOperationException: Startup assembly Microsoft.WebTools.ApiEndpointDiscovery failed to execute. See the inner exception for more details.
       ---> System.IO.FileNotFoundException: Could not load file or assembly 'Microsoft.WebTools.ApiEndpointDiscovery, Culture=neutral, PublicKeyToken=null'. The system cannot find the file specified.
      File name: 'Microsoft.WebTools.ApiEndpointDiscovery, Culture=neutral, PublicKeyToken=null'
         at System.Reflection.RuntimeAssembly.InternalLoad(AssemblyName assemblyName, StackCrawlMark& stackMark, AssemblyLoadContext assemblyLoadContext, RuntimeAssembly requestingAssembly, Boolean throwOnFileNotFound)
         at System.Reflection.Assembly.Load(AssemblyName assemblyRef)
         at Microsoft.AspNetCore.Hosting.GenericWebHostBuilder.ExecuteHostingStartups()
         --- End of inner exception stack trace ---
