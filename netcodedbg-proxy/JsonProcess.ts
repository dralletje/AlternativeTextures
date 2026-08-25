export type DapMessage = {
  seq?: number;
  type?: string;
  [key: string]: unknown;
};

function indexOfSequence(buf: Uint8Array, seq: number[]): number {
  for (let i = 0; i <= buf.length - seq.length; i++) {
    let match = true;
    for (let j = 0; j < seq.length; j++) {
      if (buf[i + j] !== seq[j]) {
        match = false;
        break;
      }
    }
    if (match) return i;
  }
  return -1;
}

export function createDapDecoder(): TransformStream<Uint8Array, DapMessage> {
  let buffer = new Uint8Array(0);
  const decoder = new TextDecoder();

  return new TransformStream({
    transform(chunk, controller) {
      const newBuf = new Uint8Array(buffer.length + chunk.length);
      newBuf.set(buffer, 0);
      newBuf.set(chunk, buffer.length);
      buffer = newBuf;

      while (true) {
        const headerEnd = indexOfSequence(buffer, [13, 10, 13, 10]); // \r\n\r\n
        if (headerEnd === -1) break;

        const headerStr = decoder.decode(buffer.subarray(0, headerEnd));
        const match = headerStr.match(/Content-Length:\s*(\d+)/i);
        if (!match) {
          buffer = buffer.subarray(headerEnd + 4);
          continue;
        }

        const contentLength = Number.parseInt(match[1], 10);
        const totalLength = headerEnd + 4 + contentLength;
        if (buffer.length < totalLength) break;

        const bodyStr = decoder.decode(buffer.subarray(headerEnd + 4, totalLength));
        buffer = buffer.subarray(totalLength);

        controller.enqueue(JSON.parse(bodyStr));
      }
    }
  });
}

export function createDapEncoder(): TransformStream<DapMessage, Uint8Array> {
  const encoder = new TextEncoder();
  return new TransformStream({
    transform(msg, controller) {
      const body = encoder.encode(JSON.stringify(msg));
      const header = encoder.encode(`Content-Length: ${body.length}\r\n\r\n`);

      const out = new Uint8Array(header.length + body.length);
      out.set(header, 0);
      out.set(body, header.length);

      controller.enqueue(out);
    }
  });
}

export function createDapProxy({
  clientIn,
  clientOut,
  serverIn,
  serverOut,
}: {
  clientIn: ReadableStream<Uint8Array>,
  clientOut: WritableStream<Uint8Array>,
  serverIn: ReadableStream<Uint8Array>,
  serverOut: WritableStream<Uint8Array>
}) {
  const encoderToServer = createDapEncoder();
  const encoderToClient = createDapEncoder();

  encoderToServer.readable.pipeTo(serverOut);
  encoderToClient.readable.pipeTo(clientOut);

  return {
    server: {
      readable: serverIn.pipeThrough(createDapDecoder()),
      writable: encoderToServer.writable,
    },
    client: {
      readable: clientIn.pipeThrough(createDapDecoder()),
      writable: encoderToClient.writable,
    },
  };
}
