const net = require('net');

let socket = null;

let bufferOffset = 0;

const HARMQUERY_PACKET_ID = 0xFA;

function onData(data) {
    bufferOffset = 0;
    let packet = data.readUInt8(bufferOffset); bufferOffset++;

    if (packet != HARMQUERY_PACKET_ID) {
        return;
    }

    socket.destroy();

    let name = readStrLp(data);
    let motd = readStrLp(data);
    let desc = readStrLp(data);
    let maxPlayers = data.readUInt32BE(bufferOffset); bufferOffset += 4;
    let players = data.readUInt32BE(bufferOffset); bufferOffset += 4;
    let playerData = [];

    for (let i = 0; i < players; i++) {
        playerData[i] = {
            'name': readStrLp(data)
        }
    }

    console.log('Name: ' + name);
    console.log('MotD: ' + motd);
    console.log('Desc: ' + desc);
    console.log('');
    console.log('Players: ' + players + ' / ' + maxPlayers);

    for (let i = 0; i < players; i++) {
        console.log('    ' + playerData[i].name);
    }
}

function onConnect() {
    console.log('Connected to server');

    let buf = Buffer.alloc(1);
    buf.writeUInt8(HARMQUERY_PACKET_ID);
    socket.write(buf);
}

function readStrLp(buf) {
    let len = buf.readUInt16BE(bufferOffset); bufferOffset += 2;
    let str = buf.toString('ascii', bufferOffset, bufferOffset + len); bufferOffset += len;
    return str;
}

function main(args) {
    let host = args[0];
    let port = parseInt(args[1]) || 25565;

    socket = new net.Socket();
    socket.on('data', onData);
    socket.connect({
        host: host,
        port: port
    }, onConnect);
}

let args = process.argv;
while (args.includes(__filename)) args.shift();
main(args);