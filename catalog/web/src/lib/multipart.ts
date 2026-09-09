import Busboy from "busboy";

export type ParsedUpload = {
  fields: Record<string, string>;
  file?: {
    fieldName: string;
    fileName: string;
    mimeType: string;
    data: Buffer;
  };
};

export function parseMultipart(req: import("node:http").IncomingMessage): Promise<ParsedUpload> {
  return new Promise((resolve, reject) => {
    const contentType = req.headers["content-type"] ?? "";
    if (!contentType.includes("multipart/form-data")) {
      reject(new Error("Expected multipart/form-data"));
      return;
    }

    const fields: Record<string, string> = {};
    let file: ParsedUpload["file"];

    const busboy = Busboy({
      headers: req.headers,
      limits: { fileSize: 8 * 1024 * 1024, files: 1 },
    });

    busboy.on("field", (name, value) => {
      fields[name] = value;
    });

    busboy.on("file", (fieldName, stream, info) => {
      const chunks: Buffer[] = [];
      stream.on("data", (chunk: Buffer) => chunks.push(chunk));
      stream.on("limit", () => reject(new Error("Image file too large")));
      stream.on("end", () => {
        file = {
          fieldName,
          fileName: info.filename,
          mimeType: info.mimeType,
          data: Buffer.concat(chunks),
        };
      });
    });

    busboy.on("error", reject);
    busboy.on("finish", () => resolve({ fields, file }));
    req.pipe(busboy);
  });
}
