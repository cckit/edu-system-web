var path = require('path');
var express = require('express');
var app = express();
var server = require('http').Server(app);

var isInit = false;

function serveStaticContent(app) {
    app.use(express.static(path.join(__dirname, 'public')));

    var contents = [{
        url: '/socket.io.js',
        path: '/node_modules/socket.io/node_modules/socket.io-client/socket.io.js'
    }, {
        url: '/jquery.min.js',
        path: '/bower_components/jquery/dist/jquery.min.js'
    }];

    contents.forEach(function(content) {
        app.get(content.url, function(req, res) {
            res.sendFile(__dirname + content.path);
        });
    });
}

function configSocketIO(server) {
    var io = require('socket.io')(server);

    var clients = {};

    io.on('connection', function(socket) {
        var client_id = "";

        socket.on('error', function(err) {
            console.error(JSON.stringify(err));
        });

        socket.on('disconnect', function() {
            console.log(client_id + ' disconnected');
        });

        socket.on('whoami', function(id) {
            if (id === 'back' || id === 'front') {
                clients[id] = socket;
                client_id = id;
                console.log(id + ' connected');
            }
        });

        socket.on('kinect-image', function(data) {
            if (clients['front'] !== undefined && data !== undefined) {
                clients['front'].emit('kinect-image', data.toString('base64'));
            }
        });
    });
}

function init() {
    serveStaticContent(app);
    configSocketIO(server);
}

module.exports = {
    start: function(port) {
        if (!isInit) {
            init();
            isInit = true;
        }

        server.listen(port);
    }
};
