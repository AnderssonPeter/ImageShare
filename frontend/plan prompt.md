Create a plan for an application to browse and download images, the backend implementation is already finished, the openapi specification can be found under `ImageShare\openapi.json`, a handof specification has been created under `frontend\BACKEND_HANDOFF.md`.
The application should use React + Compiler, Generate a client from the openapi specification (Use orval or @hey-api/openapi-ts), TanStack Router, TanStack Query, TanStack Virtual, shadcn-ui.
The layout should use the style of Metro UI (Windows 8 - Windows Phone 8.
Use the /content/{path} or /content to get folders, then use /content/random/{folder}?Thumbnail=true to get a image for the folder.
use /content/image/{path}?Thumbnail=true to get a thumbnail for the image.
Images/folders should be showed in a grid, use pagination incombination with TanStack Virtual to autoload more content.
When clicking a image, a fullscreen Carousel should show the image and allow to move the image.
If the user is Admin, there should be a button to create a link/qr image that can be sent to a different user so that the can view images
