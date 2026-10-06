import { readdirSync, readFileSync, writeFileSync } from 'node:fs';
import { join, relative, sep } from 'node:path';
import { crc32 } from 'node:zlib';

/**
 * Writes the files of a folder to a zip, stored without compression, as an author would share a
 * pack: enough for the server, which reads any zip, without a dependency.
 */
export function zipFolder(folder: string, archive: string): void {
    const locals: Buffer[] = [];
    const centrals: Buffer[] = [];
    let offset = 0;
    for (const file of readdirSync(folder, { recursive: true, withFileTypes: true })) {
        if (!file.isFile()) {
            continue;
        }
        const path = join(file.parentPath, file.name);
        const name = Buffer.from(relative(folder, path).split(sep).join('/'));
        const data = readFileSync(path);
        // Version 2.0, UTF-8 names, stored, 1980-01-01 00:00, CRC, both sizes, name length, no extra.
        const fields = Buffer.alloc(26);
        fields.writeUInt16LE(20, 0);
        fields.writeUInt16LE(0x0800, 2);
        fields.writeUInt16LE(0x21, 8);
        fields.writeUInt32LE(crc32(data), 10);
        fields.writeUInt32LE(data.length, 14);
        fields.writeUInt32LE(data.length, 18);
        fields.writeUInt16LE(name.length, 22);
        locals.push(signature(0x04034b50), fields, name, data);
        const central = Buffer.alloc(42);
        central.writeUInt16LE(20, 0);
        fields.copy(central, 2);
        central.writeUInt32LE(offset, 38);
        centrals.push(signature(0x02014b50), central, name);
        offset += 30 + name.length + data.length;
    }
    const directory = Buffer.concat(centrals);
    const end = Buffer.alloc(18);
    const count = centrals.length / 3;
    end.writeUInt16LE(count, 4);
    end.writeUInt16LE(count, 6);
    end.writeUInt32LE(directory.length, 8);
    end.writeUInt32LE(offset, 12);
    writeFileSync(archive, Buffer.concat([...locals, directory, signature(0x06054b50), end]));
}

function signature(value: number): Buffer {
    const buffer = Buffer.alloc(4);
    buffer.writeUInt32LE(value);
    return buffer;
}
