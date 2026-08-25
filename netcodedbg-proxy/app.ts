#!/usr/bin/env node

import { spawn } from "node:child_process";
import { Readable, Writable } from "node:stream";
import { createWriteStream } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";
import { Console } from "console";
import { createDapProxy } from "./JsonProcess.ts";
import find from "find-process";
import { produce } from "immer";

const __dirname = dirname(fileURLToPath(import.meta.url));
const logStream = createWriteStream(join(__dirname, "dap-proxy.log"), {
  flags: "a",
});

function log(direction: string, msg: unknown) {
  logStream.write(`[${direction}] ${JSON.stringify(msg, null, 2)}\n\n`);
}

const console = new Console(process.stderr);

const adapter = spawn("netcoredbg", ["--interpreter=vscode"], {
  stdio: ["pipe", "pipe", "inherit"],
});

console.log("#1");

const { server, client } = createDapProxy({
  clientIn: Readable.toWeb(process.stdin) as ReadableStream<Uint8Array>,
  clientOut: Writable.toWeb(process.stdout) as WritableStream<Uint8Array>,
  serverIn: Readable.toWeb(adapter.stdout) as ReadableStream<Uint8Array>,
  serverOut: Writable.toWeb(adapter.stdin) as WritableStream<Uint8Array>,
});

let serverWriter = server.writable.getWriter();
let clientWriter = client.writable.getWriter();

(async () => {
  for await (const msg of client.readable) {
    if (msg.command === "attach") {
      if (msg.arguments?.processName != null) {
        var processName = msg.arguments?.processName;
        var result = await find("name", processName);
        var processId = result?.[0]?.pid;
        console.log(`Corrected ${processName} to ${processId}`);
        if (processId) {
          var newmsg = produce(msg, (draft) => {
            draft.arguments.processId = `${processId}`;
            delete draft.arguments.processName;
          });
          console.log("New msg", newmsg);
          await serverWriter.write(newmsg);
          continue;
        }
      }
    }

    log("ZED -> DBG", msg);
    await serverWriter.write(msg);
  }
})();

type StackTraceCommand = {
  command: "stackTrace";
  request_seq: number;
  seq: number;
  success: boolean;
  type: "response";
  body: { stackFrames: StackFrame[]; totalFrames: number };
};

type StackFrame = {
  column: number;
  endColumn: number;
  endLine: number;
  id: number;
  line: number;
  moduleId: string;
  name: string;
  source: {
    name: string;
    path: string;
  };
};

(async () => {
  for await (const msg of server.readable) {
    log("DBG -> ZED", msg);

    if (msg.command === "stackTrace") {
      console.log("msg:", msg);
      const stackTrace = msg as StackTraceCommand;
      if (stackTrace.body != null) {
        const { stackFrames, totalFrames } = stackTrace.body;

        var stardewSource =
          "/Users/michiel/Projects/AlternativeTextures/Decompiled";
        var newmsg = produce(stackTrace, (draft) => {
          for (const frame of stackFrames) {
            if (frame.source.path.startsWith("StardewValley")) {
              frame.source.path = `${stardewSource}/${frame.source.path}`;
            }
          }
        });
        await clientWriter.write(newmsg);
        continue;
      }
    }
    await clientWriter.write(msg);
  }
})();

console.log("#2");

adapter.on("exit", (code) => {
  console.log("#3 EXIT");
  logStream.end();
  process.exit(code ?? 0);
});
process.stdin.on("end", () => {
  console.log("#4 STDIN END");
  serverWriter.close();
});
