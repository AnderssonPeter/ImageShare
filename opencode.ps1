docker run -it --rm `
  --cap-drop all --security-opt=no-new-privileges:true `
  -v ./opencode/share:/home/opencode/.local/share/opencode `
  -v ./opencode/state:/home/opencode/.local/state/opencode `
  -v ./opencode/config:/home/opencode/.config/opencode `
  -v .:/app `
  -p 19876:19876 `
  opencode-image-share:latest

# run as readonly
# find all package.json and map node_module to tmp fs
# find all *.csproj and map obj, bin to tmp fs