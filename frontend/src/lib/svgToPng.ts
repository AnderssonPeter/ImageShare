/**
 * SvgToPng — rasterize an inline <svg> element to a PNG File.
 *
 * Serializes the SVG to a blob, decodes it via `createImageBitmap`, and
 * draws it onto a canvas at the requested pixel size. The blob inherits
 * the page origin, so same-origin subresources referenced by the SVG
 * (e.g. an embedded logo <image>) stay resolvable without tainting the
 * canvas.
 */
const SVG_TYPE = "image/svg+xml";
const PNG_TYPE = "image/png";

function sizedCanvas(size: number): HTMLCanvasElement {
  const canvas = document.createElement("canvas");
  canvas.width = size;
  canvas.height = size;
  return canvas;
}

async function rasterizeToCanvas(svg: SVGSVGElement, size: number): Promise<HTMLCanvasElement> {
  const source = new XMLSerializer().serializeToString(svg);
  const svgBlob = new Blob([source], { type: SVG_TYPE });
  const bitmap = await createImageBitmap(svgBlob);
  const canvas = sizedCanvas(size);
  const context = canvas.getContext("2d");
  if (context === null) {
    throw new Error("Canvas 2D context is unavailable.");
  }
  context.drawImage(bitmap, 0, 0, size, size);
  bitmap.close();
  return canvas;
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
