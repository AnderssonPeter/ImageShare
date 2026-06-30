docker run -it --rm `
  --cap-drop all --security-opt=no-new-privileges:true `
  -v ./opencode/share:/home/opencode/.local/share/opencode `
  -v ./opencode/state:/home/opencode/.local/state/opencode `
  -v ./opencode/config:/home/opencode/.config/opencode `
  -v .:/app opencode-image-share:latest