/**
 * SvgToPng — rasterize an inline <svg> element to a PNG File.
 *
 * Serializes the SVG to a blob, loads it through an `<img>` element, and
 * draws it onto a canvas at the requested pixel size. Using `<img>` (rather
 * than `createImageBitmap`) leverages the browser's full image-loading
 * pipeline, which reliably decodes SVG blobs — including those with
 * embedded data-URI subresources (e.g. a logo `<image>`) — whereas
 * `createImageBitmap` can fail with "The source image could not be decoded"
 * for SVG inputs in some browsers.
 */
const SVG_TYPE = "image/svg+xml";
const PNG_TYPE = "image/png";

function sizedCanvas(size: number): HTMLCanvasElement {
  const canvas = document.createElement("canvas");
  canvas.width = size;
  canvas.height = size;
  return canvas;
}

async function loadSvgImage(url: string): Promise<HTMLImageElement> {
  const image = new Image();
  image.src = url;
  await image.decode();
  return image;
}

async function rasterizeToCanvas(svg: SVGSVGElement, size: number): Promise<HTMLCanvasElement> {
  const source = new XMLSerializer().serializeToString(svg);
  const svgBlob = new Blob([source], { type: SVG_TYPE });
  const url = URL.createObjectURL(svgBlob);
  try {
    const image = await loadSvgImage(url);
    const canvas = sizedCanvas(size);
    const context = canvas.getContext("2d");
    if (context === null) {
      throw new Error("Canvas 2D context is unavailable.");
    }
    context.drawImage(image, 0, 0, size, size);
    return canvas;
  } finally {
    URL.revokeObjectURL(url);
  }
}

async function canvasToPngFile(canvas: HTMLCanvasElement, filename: string): Promise<File> {
  const dataUrl = canvas.toDataURL(PNG_TYPE);
  const response = await fetch(dataUrl);
  const pngBlob = await response.blob();
  return new File([pngBlob], filename, { type: PNG_TYPE });
}

export async function svgElementToPngFile(
  svg: SVGSVGElement,
  size: number,
  filename: string,
): Promise<File> {
  const canvas = await rasterizeToCanvas(svg, size);
  return canvasToPngFile(canvas, filename);
}
