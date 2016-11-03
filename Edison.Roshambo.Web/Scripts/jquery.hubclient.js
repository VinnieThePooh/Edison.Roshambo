function addUserToTable(table, user) {
    var row = $("<tr>");
// add icon 
//     row.append($("<td>").text(user.UserName));
    row.append($("<td>").text(user.UserName));
    table.append(row);
}

function addLobbyToTable(table, lobby) {
    var manager = window.GameManager;
    var currentGame = manager.currentGame;
    var currentUserName = window.UserName;

    if (lobby.LobbyOwner == currentUserName) {
        currentGame.LobbyOwnerName= currentUserName;
    }


    var row = $("<tr>").addClass("clickable-row").attr("id",lobby.LobbyId);
    row.append($("<td>").text(lobby.LobbyName));
    row.append($("<td>").text(lobby.LobbyOwner));

    if (lobby.LobbyState === window.Resources.LobbyStateBlocked) {
        row.append($("<td>").addClass("text-danger").text(lobby.LobbyState));
    }
    else row.append($("<td>").text(lobby.LobbyState));
    
    row.append($("<td>").text(lobby.OpponentName));
    table.append(row);
}

function defineClientCallbacks(gamesHub) {
    var client = gamesHub.client;
    client.newUserAdded = onNewUserAdded;
    client.userLeft = onUserLeft;
    client.tryCreateLobby = onCreateLobby;
    client.tryRemoveLobby = onRemoveLobby;
    // callback for who is joining lobby
    client.lobbyJoined = onLobbyJoined;
    // callback for lobby owner
    client.lobbyWasJoined = onLobbyWasJoined;
    // callback for others 
    client.lobbyWasJoinedAll = onLobbyWasJoinedAll;
    client.lobbyStateChanged = onLobbyStateChanged;
    client.addLobbyMessage = onAddLobbyMessage;
    client.gameStarted = onGameStarted;
    client.lobbyOwnerLeftTheGame = onLobbyOwnerLeftTheGame;
    client.playerLeftTheGame = onPlayerLeftTheGame;
    client.roundStarted = onRoundStarted;
    client.userHasBeenBlocked = onUserHasBeenBlocked;
    client.userHasBeenUnblocked = onUserHasBeenUnblocked;
    client.lobbyHasBeenBlocked = onlobbyHasBeenBlocked;
    client.lobbyHasBeenBlockedAll = onLobbyHasBeenBlockedAll;
    client.lobbyHasBeenUnblockedAll = onLobbyHasBeenUnblockedAll;
    client.shapeWasSent = onShapeWasSent;
    client.roundEnded = onRoundEnded;
}

function onRoundEnded(data) {

    var manager = window.GameManager;
    var ownerShapeId = data.OwnerShapeId;
    var opponentShapeId = data.OpponentShapeId;
    var winner = data.WinnerUsername;

    var mapper = manager.ImagesIdToShapesIdMapper;
    var imgSourceUrl;

    for (var prop in mapper) {
        if (mapper[prop] === opponentShapeId) {
            imgSourceUrl = $("#" + prop).attr("src");
            $("#imgOpponentShape").attr("src", imgSourceUrl);
            break;
        }
    }

    // define winner

}


function onShapeWasSent(gameId, roundNumber, shapeId, error) {

    if (error) {
        console.log(error);
        return;
    }

    var manager = window.GameManager;
    var game = manager.currentGame;
    game.rounds[roundNumber - 1].shapeWasSent = true;
    
    // set image here

    var mapper = manager.ImagesIdToShapesIdMapper;
    var imgSourceUrl;

    for (var prop in mapper) {
        if (mapper[prop] === shapeId) {
            imgSourceUrl = $("#" + prop).attr("src");
            $("#imgYourShape").attr("src",imgSourceUrl);
            break;
        }
    }

}



function onLobbyHasBeenUnblockedAll(data) {
    
}



function onLobbyHasBeenBlockedAll(data) {
    // message + blocking ui
}


function onlobbyHasBeenBlocked(data) {
    var message = data.Message;
    var p = $("#messageUsers");
    // message to lobby owner
    setTempMessage(p, message);
    // + blocking ui
    // start button only
    // + add some icon
}


function onUserHasBeenUnblocked(message) {
    var p = $("#messageUsers");
    setTempMessage(p, message);
    var manager = window.GameManager;
    manager.userBlocked = false;
    manager.BlockingTime = null;
    $("#btnJoinToLobby").prop("disabled", false);
    $("#btnPlay").prop("disabled", false);
    $("#btnCreateLobby").prop("disabled", false);
    $("#btnLeaveJoinedLobby").prop("disabled", false);
}


function onUserHasBeenBlocked(data) {
    var p = $("#messageUsers");
    setTempMessage(p, data.Message);
    // setTimeOut for blocking time
    // add red indicator
    var manager = window.GameManager;
    $("#btnJoinToLobby").prop("disabled", true);
    $("#btnPlay").prop("disabled", true);
    $("#btnCreateLobby").prop("disabled", true);
    $("#btnLeaveJoinedLobby").prop("disabled", true);
    manager.userBlocked = true;
    manager.BlockingTime = new Date();
    beginCountDownToUnblock(5);
}

function onRoundStarted(data) {
     
}

function onLobbyWasBlocked(data) {
    var name = data.LobbyName;
    var time = data.BlockingTime;
    var tr = $("#tableLobbies").find("tr").filter(function() {
        return this.cells[0].textContent === name;
    });
    var span = $("<span>").css("color", "red").css("font-weight", "bold").html(window.Resources.LobbyStateBlocked);
    tr.find("td").get(2).append(span);
}

function onPlayerLeftTheGame(data) {
    var visible = $('#playingModal').is(':visible');
    if (visible) {
        var p = $("#gameMessage");
        setTempMessage(p, data.Message);
        setTimeout(function () {
            $('#playingModal').modal('hide');
        }, 3000);
    }
}

function onLobbyOwnerLeftTheGame(data) {
    var visible = $('#playingModal').is(':visible');
    if (visible) {
        var p = $("#gameMessage");
        setTempMessage(p, data.Message);
        setTimeout(function() {
            $('#playingModal').modal('hide');
        }, 3000);
    }
}

// just ui
// lobbyname should be checked
function onGameStarted(data) {
    var manager = window.GameManager;
    var game = manager.currentGame;

    game.gameId = data.GameId;
    game.lobbyOwnerName = data.LobbyOwnerName;
    game.opponentName = data.OpponentName;
    game.lobbyName = data.LobbyName;
    
    $("#playingModal").modal({ backdrop: "static" });
    $("#playingModal").data("lobbyname", data.LobbyName);

    //init ui here 
    $("#tableScores .owner").first().html(data.LobbyOwnerName);
    $("#tableScores .opponent").first().html(data.OpponentName);


    manager.startPlayingIteration(1);
}

function onLobbyWasJoinedAll(data) {

    var opName = data.OpponentName;
    var lobbyId = data.LobbyId;

    var row = $("#tableLobbies").find("tr#" + lobbyId);
    var td = row.find("td").get(3);
    $(td).html(opName);
}

function onAddLobbyMessage(message)
{
    
}

// callback when trying to join lobby
function onLobbyJoined(data) {

    var manager = window.GameManager;
    var game = manager.currentGame;

    game.opponentName = data.OpponentName;
    game.currentUserName = data.CurrentUserName;
    game.LobbyName = data.LobbyName;
    game.LobbyOwnerName = data.OpponentName;
    var lobbyId = data.LobbyId;
    var tblOpponentName = data.CurrentUserName;

    var row = $("#tableLobbies").find("tr#" + lobbyId);
    var td = row.find("td").get(3);
    $(td).html(tblOpponentName);

    var par = $("#messageLobbies");
    $("#joinLobbyModal").modal("hide");
    setTempMessage(par, data.Message);
}


function onLobbyWasJoined(data) {
    var manager = window.GameManager;
    var lobbyId = data.LobbyId;
    
    var game = manager.currentGame;
    game.currentUserName = data.CurrentUserName;
    game.LobbyOwnerName = data.CurrentUserName;
    game.opponentName = data.OpponentName;
    game.LobbyName = data.LobbyName;


    var row = $("#tableLobbies").find("tr#" + lobbyId);
    var oppTd = row.find("td").get(3);
    $(oppTd).text(data.OpponentName);

    var message = data.Message;
    var par = $("#messageLobbies");
    setTempMessage(par, message);
}




function onUserLeft(user) {
    var par = $("#messageUsers");
    var row = $("#tableUsers td").filter(function() {
        return $(this).text() == user.UserName;
    }).closest("tr");
    row.remove();
    var message = "User " + $("<span>").attr("style", "color:red;font-style:italic;").text(user.UserName).text() + " has left playing room.";
    setTempMessage(par, message,2);
 }

function onNewUserAdded(user) {
    var par = $("#messageUsers");
    var table = $("#tableUsers");
    addUserToTable(table, user);
    var message = "User " + $("<span>").attr("style", "color:red;font-style:italic;").text(user.UserName).text() + " has joined playing room.";
    setTempMessage(par, message, 2);
}

function onCreateLobby(response) {

    var par = $("#messageLobbies").text(null);
    var lobbyError = $("#lobbyError").text(null);
    var parentInput = $("#lobbyName").parent().removeClass("has-error").removeClass("has-success");
    
    if (response.Result) {
        $("#modalWindow").modal("hide");
        setTempMessage(par, response.Message);
        var table = $("#tableLobbies");
        addLobbyToTable(table, response.Lobby);
        window.storage.addLobby(response.Lobby);
    } else {
        parentInput.addClass("has-error");
        setTempMessage(lobbyError, response.Message);
    }
}

function onRemoveLobby(response) {
    var lobbyError = $("#lobbyError").text(null);
    if (response.Result) {
        var id = response.LobbyId;
        var row = $("#tableLobbies").find("tr#" + id);
        row && row.remove();
     }
}


function onLobbyStateChanged(lobby) {
    var row = $("#tableLobbies").find("tr#" + lobby.LobbyId);
    var td = row.find("td").get(2);
    $(td).removeClass("text-danger").html(lobby.LobbyState);

    if (lobby.LobbyState === window.Resources.LobbyStateAwaitingForPlayers) {
        td = row.find("td").get(3);
        $(td).html(null);
    }

    if (lobby.LobbyState === window.Resources.LobbyStatePlaying) {
        
    }

    if (lobby.LobbyState === window.Resources.LobbyStateReadyToStart) {
        
    }

    if (lobby.LobbyState === window.Resources.LobbyStateSummarizing) {
        
    }

    if (lobby.LobbyState === window.Resources.LobbyStateBlocked) {
          $(td).addClass("text-danger");
    }

    $("#joinLobbyModal").modal("hide");
}

function setTempMessage(paragraph, message, interval) {
    interval = interval || 3;
    paragraph.text(message);
    var counter = 0;
    
    var timer = setInterval(function () {
        if (counter++ == interval) {
            paragraph.text(null);
            clearInterval(timer);
        };
    }, 1000);
}


