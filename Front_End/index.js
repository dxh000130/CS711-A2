const board = document.getElementById("chess-board");
let _username;
let _gameId;
let _pairCheck = false;
let _Player = ""
let _Dargava;
let _lastTime;
function drawBoard() {
    const initialSetup = [
        ["Rb", "Nb", "Bb", "Qb", "Kb", "Bb", "Nb", "Rb"],
        ["Pb", "Pb", "Pb", "Pb", "Pb", "Pb", "Pb", "Pb"],
        [null, null, null, null, null, null, null, null],
        [null, null, null, null, null, null, null, null],
        [null, null, null, null, null, null, null, null],
        [null, null, null, null, null, null, null, null],
        ["Pw", "Pw", "Pw", "Pw", "Pw", "Pw", "Pw", "Pw"],
        ["Rw", "Nw", "Bw", "Qw", "Kw", "Bw", "Nw", "Rw"]
    ];
    let sameCount = 0;
    for (let row = 0; row < 8; row++) {
        for (let col = 0; col < 8; col++) {
            sameCount++;
            const piece = initialSetup[row][col];
            const cell = `<div class="chess-cell ${(row + col) % 2 === 0 ? "light" : "dark"}" id="${row}-${col}" ondragover="onDragOver(event)" ondrop="onDrop(event)">
            ${(piece ? `<div class="chess-piece" id="${piece}${sameCount}${(piece[1]==='w'?'P1':'P2')}" draggable="true" style="background-image: url(https://cws.auckland.ac.nz/gas/images/${piece}.svg)" ondragstart="onDragStart(event)"></div>`:'')}
            </div>`;

            board.innerHTML += cell;
        }
    }
}


function onDragOver(event) {
    event.preventDefault();
}

function onDragStart(event) {
    if(_username && _gameId){
        if ( _Dargava){
            if (event.target.classList.contains("chess-piece")){
                if(event.target.id.slice(event.target.id.length-2, ).toUpperCase() === _Player){
                    console.log(222)
                    event.dataTransfer.setData("text/plain", event.target.id);
                }
            }
        }else {
            alert("Please get their move first!")
        }
    }else {
        alert("Please pair game first!")
    }
}

function onDrop(event) {
    event.preventDefault();
    const pieceId = event.dataTransfer.getData("text/plain");
    const piece = document.getElementById(pieceId);
    if (piece) {
        const targetCell = event.target.closest(".chess-cell");
        if (targetCell) {
            targetCell.innerHTML = "";
            targetCell.appendChild(piece);
            sendMyMove();
        }
    }
}
function getBasicInfo(){
    fetch('http://localhost:8080/favicon.ico')
        .then(response => response.blob())
        .then(blob => {
            // 处理获取到的 favicon 图标数据
            const url = URL.createObjectURL(blob);
            const favicon = document.createElement('link');
            favicon.rel = 'icon';
            favicon.type = 'image/x-icon';
            favicon.href = url;
            document.head.appendChild(favicon);
        })
        .catch(error => {
            // 处理获取 favicon 图标失败的情况
            console.error(error);
        });
    fetch('http://localhost:8080/version')
        .then(response => response.text())
        .then(text => {
            document.getElementById("version").innerText = "Version: "+text;
        })
        .catch(error => {

            console.error(error);
        });
    fetch('http://localhost:8080/debug')
        .then(response => response.text())
        .then(text => {
            document.getElementById("debug").innerText = text;
        })
        .catch(error => {

            console.error(error);
        });
}
function getUsername() {
    if (_username){
        alert("You have a username, cannot get again!")
    }else {
        fetch('http://localhost:8080/register')
            .then(response => response.text())
            .then(text => {
                document.getElementById("username").innerText = "Username:"+text;
                _username = text
            })
            .catch(error => {
                console.error(error);
            });
    }

}
function sendMyMove() {
    if(_username && _gameId){
        _Dargava = false;
        document.getElementById("theirMove").style.backgroundColor='green'
        if(_Player.toUpperCase() === "P1"){
            document.getElementById("status").innerText = "You are Player1, your chess pieces are white, Player2 go, wait for Player2!";
        }else {
            document.getElementById("status").innerText = "You are Player2, your chess pieces are black, Player1 go, wait for Player2!";
        }
        let boardState = [];
        for (let row = 0; row < 8; row++) {
            let rowData = [];
            for (let col = 0; col < 8; col++) {
                let cellId = `${row}-${col}`;
                let cell = document.getElementById(cellId);
                let piece = cell.querySelector('.chess-piece');
                rowData.push(piece ? piece.id : null);
            }
            boardState.push(rowData);

        }
        const now = new Date();
        boardState.push(now)
        fetch(`http://localhost:8080/mymove?player=${_username}&id=${_gameId}&move=${JSON.stringify(boardState)}`)
            .then(response => response.text())
            .then(text => {
                document.getElementById("message").innerText="Move has been send!"
                setTimeout(function() {
                    document.getElementById("message").innerText=""
                }, 3000);
            })
            .catch(error => {
                console.error(error);
            });
    }else {
        alert("Please pair game first!")
    }

}

function getTheirMove() {
    if(_username && _gameId){
        if (!_Dargava){
            fetch(`http://localhost:8080/theirmove?player=${_username}&id=${_gameId}`)
                .then(response => response.text())
                .then(boardState => {
                    if (boardState !== ""){
                        boardState = JSON.parse(boardState);
                        const time = boardState.pop();
                        if (time !== _lastTime){
                            _lastTime = time;
                            document.getElementById("theirMove").style.backgroundColor='#f0f0f0'
                            _Dargava = true;
                            if(_Player.toUpperCase() === "P1"){
                                document.getElementById("status").innerText = "You are Player1, your chess pieces are white, your go!";
                            }else {
                                document.getElementById("status").innerText = "You are Player2, your chess pieces are black, your go!";
                            }


                            const board = document.getElementById('chess-board');
                            board.innerHTML = '';
                            for (let row = 0; row < 8; row++) {
                                for (let col = 0; col < 8; col++) {
                                    const pieceId = boardState[row][col];
                                    board.innerHTML += `
                        <div class="${((row + col) % 2 === 0 ? 'chess-cell light' : 'chess-cell dark')}" id="${row}-${col}" ondragover="onDragOver(event)" ondrop="onDrop(event)">
                        ${(pieceId ? `
                        <div class="chess-piece" id="${pieceId}" draggable="true" style="background-image: url(https://cws.auckland.ac.nz/gas/images/${pieceId.slice(0, 2)}.svg)" ondragstart="onDragStart(event)"></div>
                        ` : '')}
                        </div>
                        `;
                                }
                            }
                        }

                    }

                })
                .catch(error => {
                    console.log(error);
                });
        }

    }else {
        alert("Please pair game first!")
    }
}


function pairMe() {
    if(_username){

        fetch('http://localhost:8080/pairme?player='+_username)
            .then(response => response.json())
            .then(json => {
                console.log(json)
                if(json.state === "wait"){
                    _pairCheck=true;
                    document.getElementById("status").innerText = "Wait for another player!"
                }else if(json.state === "progress"){
                    _pairCheck=false;
                    _gameId = json.id;
                    if(json.player1 === _username){
                        document.getElementById("status").innerText = "You are Player1, your chess pieces are white, your first";
                        _Player = "P1"
                        _Dargava = true;
                    }else if(json.player2 === _username){
                        document.getElementById("status").innerText = "You are Player2, your chess pieces are black, Player1 first";
                        document.getElementById("theirMove").style.backgroundColor='green'
                        _Player = "P2"
                        _Dargava = false;
                    }
                }

            })
            .catch(error => {
                console.error(error);
            });
    }else {
        alert("Please get username first!")
    }

}
function quit() {
    if(_username && _gameId){
        fetch(`http://localhost:8080/quit?player=${_username}&id=${_gameId}`)
            .then(response => response.text())
            .then(text => {
                document.getElementById("status").innerText = "Log out successful"
                document.getElementById("username").innerText = "";
                _username=null;
                _gameId=null;
                _pairCheck = false;
                _Player = ""
                _Dargava=null;
            })
            .catch(error => {
                console.error(error);
            });
    }else {
        alert("Please pair game first!")
    }
}

drawBoard();
getBasicInfo();

setInterval(function() {
    if(_pairCheck){
        pairMe();
    }
    if(_gameId){
        getTheirMove();
    }
}, 1000);

