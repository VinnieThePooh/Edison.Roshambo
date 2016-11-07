function GameManager(gamesHub) {

    var UNDEFINED_SHAPE_ID = 6;
    var VICTORY_THRESHOLD = 3;

    var currentManager = this;
    this.gamesHub = gamesHub;
    this.currentLobbyRoundDuration = 5;
    this.beforeRoundsTimeout = 3;
    this.isUserLobbyOwner = false;
    this.currentRoundNumber = 0; // for tracing gaming process
    this.tipWasUsed = false;
//    this.userReloadedPage = false;
    this.userBlocked = false;


    var playingTimer = {};
    var isPlayingState = false;
    var tipsCount = 1;

    // lobby owner opponent scores
    var opponentScores = 0;
    // owner scores
    var ownerScores = 0;
    this.isUserJoinedLobby = false;


    
    this.imagesIdToShapesIdMapper = {
        img1: 1,
        img2: 2,
        img3: 3,
        img4: 4,
        img5: 5,
        "undefined": 6
    };

    this.shapesIdToNameMapper = {
        map: {
            1: "Rock",
            2: "Paper",
            3: "Scissors",
            4: "Lizard",
            5: "Spock",
            6: "Undefined"
        }
    };


    this.blockingTime = new Date();
    this.currentGame = { gameId: "", lobbyName: "", currentUserName: "", opponentName: "", rounds: [], gameState: "", lobbyOwnerName: "" };
    this.sendShape = function(gameId, roundNumber, figureId) {
        var round = this.currentGame.rounds[roundNumber - 1];
        round && !round.shapeWasSent && isPlayingState && this.gamesHub.server.sendShape(gameId, roundNumber, figureId);
    };

    this.useTip = function () {
        var gameId = this.currentGame.gameId;
        var rNumber = this.currentRoundNumber;
        tipsCount > 0 && !this.tipWasUsed && isPlayingState && gamesHub.server.useTip(gameId, rNumber);
    };

    this.unblockUser = function() {
        var manager = this;
        var hub = $.connection.hub;
        if (hub.state === $.signalR.connectionState.disconnected) {
            hub.start().done(function() {
                manager.gamesHub.server.unblockUser(manager.currentGame.currentUserName);
            }).fail(function() {
                console.log("could not connect to server");
            });
        } else {
            manager.gamesHub.server.unblockUser(manager.currentGame.currentUserName);
        }
    };
    this.gameResults = { winnerUserName: "", winnerScores: 0, opponentScores: 0 };
    this.startGame = function() {
        // use serverside method to start game
        // ss must return: gameId, lobbyOwnerName, opponentName
        // set this values to currentGame properties 
        var server = gamesHub.server;
        server.startGame(this.currentGame.opponentName);
    };
    this.startRound = function(roundNumber) {
        //this method is automatically called after previous round completed
        // logic: 1) before game countDown,  2)whileGameCountDown
        // For 2) User sends his choice or user loses the round
        var gameId = this.currentGame.gameId;
        var server = this.gamesHub.server;
        server.startRound(gameId, roundNumber);
    };

    this.endGame = function(gameId, ownerScores, opponentScores) {
        this.gamesHub.server.endGame(gameId, ownerScores, opponentScores);
    };
    this.leaveLobby = function() {
        var server = this.gamesHub.server;
        var lobbyName = this.currentGame.LobbyName;
        server.leaveLobby(lobbyName);
        $("#playingModal").modal("hide");
    };

    this.startPlayingIteration = function (roundNumber) {
        
        var game = this.currentGame;
        var round = {
            roundNumber: roundNumber,
            opponentShape: "",
            currentUserScores: currentManager.currentUserScores,
            opponentScores: currentManager.opponentScores,
            currentUserShape: "",
            opponentUsedTip: false,
            currentUserUsedTip: false,
            shapeWasSent: false
        };
        game.rounds.push(round);
        startBeforeGameCountDown(roundNumber);
    };

    defineClientCallbacks(gamesHub);

    // implementation details
    function hideShapes() {
        $("div.yourShape").css("display", "none");
        $("div.opponentShape").css("display", "none");
    }


    function resetImagesState() {
        Array.from($(".imagesList .thumbnail")).forEach(function(th) {
            $(th).find(".caption").addClass("hidden").removeClass("visible");
        });
    }


    function resetScores() {
        opponentScores = 0;
        ownerScores = 0;
    }

    function resetChosenImages() {
        $("#imgYourShape").attr("src", "/Content/Images/question-mark.png");
        $("#imgOpponentShape").attr("src", "/Content/Images/question-mark.png");
    }

    function showShapes() {
        var div = $("div.yourShape").first();
        div.css("display", "block");

        div = $("div.opponentShape").first();
        div.css("display", "block");
    }

    function hideTimer() {
        var timer = $("#tableTimer tr.time h4").first();
        timer.css("display", "none");
        var tinfo = $("#tableTimer p.text-info").first();
        tinfo.css("display", "none");
    }


    function hideAnnouncement() {
        $("#announcementHeader").find("h3").text(null);
    }

    function showTimer() {
        var timer = $("#tableTimer tr.time h4").first();
        timer.text(null);
        timer.css("display", "block");
        var tinfo = $("#tableTimer p.text-info").first();
        tinfo.css("display", "block");
    }

    function setScoresTable() {

        if (currentManager.isUserLobbyOwner) {
            $("#yourScores").html(ownerScores);
            $("#oppScores").html(opponentScores);
        } else {
            $("#yourScores").html(opponentScores);
            $("#oppScores").html(ownerScores);
        }
    }


    function startBeforeGameCountDown(roundNumber) {

        isPlayingState = false;
        hideTimer();
        hideShapes();
        hideAnnouncement();
        resetImagesState();
        resetChosenImages();

        var header = $("#announcementHeader").find("h3");
        var message = window.Resources.NextRoundWillStartIn.replace("*", roundNumber);
        var timeout = currentManager.beforeRoundsTimeout;

        var timer = setInterval(function() {
            if (timeout === 0) {
                header.text(null);
                clearInterval(timer);
                header.text("FIGHT!!!");
                // begin round timeout
                currentManager.currentRoundNumber = roundNumber;
                currentManager.startRound(roundNumber);
                isPlayingState = true;
                startWhileGameCountDown(roundNumber);
                return;
            }

            header.text(message.replace("=", timeout));
            timeout--;
        }, 1000);
    }

    function startWhileGameCountDown(roundNumber) {
        var header = $("#announcementHeader h3");
        $(".asideTimer p.text-info").text("Time left to make your choice:");
        var timeInfo = $("#tableTimer tr.time h4").first();
        currentManager.currentRoundNumber = roundNumber;
        var round = currentManager.currentGame.rounds[roundNumber - 1];

        // fix round on server

        var duration = currentManager.currentLobbyRoundDuration;
        var counter = 0;
        showTimer();
        showShapes();

        playingTimer = setInterval(function() {
            timeInfo.text(duration);
            // clear fight message
            if (counter === 1) {
                header.text(null);
            }

            if (duration === 0) {

                clearInterval(playingTimer);
                isPlayingState = false;
                hideTimer();
                if (!round.shapeWasSent) {
                    var hub = currentManager.gamesHub;
                    hub.server.sendShape(currentManager.currentGame.gameId, currentManager.currentRoundNumber, UNDEFINED_SHAPE_ID);
                }
            }
            duration--;
            counter++;
        }, 1000);
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
        client.lobbyHasBeenBlocked = onLobbyHasBeenBlocked;
        client.userHasBeenBlocked = onUserHasBeenBlocked;
        client.userHasBeenUnblocked = onUserHasBeenUnblocked;
        client.shapeWasSent = onShapeWasSent;
        client.roundEnded = onRoundEnded;
        client.correctLobbyOwning = onCorrectLobbyOwning;
        client.gameEnded = onGameEnded;
        client.tipWasUsed = onTipWasUsed;
        client.opponentUsedTip = onOpponentUsedTip;
        // this callback for user who is leaving lobby
        client.userLeftLobby = onUserLeftLobby;
        client.userLeftSite = onUserLeftSite;
        client.userJoinedSite = onUserJoinedSite;
    }

    function onUserLeftSite(data) {
        var name = data.UserName;
        var email = data.UserEmail;

        var targetRow = $("#tableUsers tr").filter(function() {
            return $(this).data("email") === email && $(this).text() === name;
        });

        if (targetRow)
            targetRow.remove();
    }


    function onUserJoinedSite(data) {

        var email = data.UserEmail;
        var name = data.UserName;

        var tableUsers = $("#tableUsers");
        var targetRow = $("#tableUsers tr").filter(function () {
            return $(this).data("email") === email && $(this).text() === name;
        });

        if (!targetRow.length) {
            var row = $("<tr></tr>").attr("data-email", data.UserEmail);
            row.append($("<td></td>").text(data.UserName));
            tableUsers.append(row);
        }
    }
    

    // empty yet
    function onLobbyHasBeenBlocked(data) {
        


    }


    // actually data is not needed here
    // todo: refactor
    function onUserLeftLobby(data) {

        if (data.Error) {
            console.log(data.Error);
            return;
        }
        
        currentManager.isUserJoinedLobby = false;
        $("#btnLeaveJoinedLobby").prop("disabled", true);
        var p = $("#messageUsers");
        setTempMessage(p, window.Resources.YouSuccessfullyLeftLobby);
    }

    function onOpponentUsedTip(data) {
        var rNumber = data.RoundNumber;
        var round = currentManager.currentGame.rounds[rNumber - 1];
        console.log("Opponent used tip at round: " + rNumber);
        round.opponentUsedTip = true;
    }


    function onTipWasUsed(data) {
        var round = currentManager.currentGame.rounds[currentManager.currentRoundNumber-1];
        
        currentManager.tipWasUsed = true;
        round.tipWasUsed = true;
        round.currentUserUsedTip = true;

        if (tipsCount > 0)
            tipsCount--;

        $("#tipsCount").text(tipsCount);

        if (data.Error) {
            console.log(data.Error);
            return;
        }

        var p = $("#gameMessage");
        var message = window.Resources.TipUsingFailed;

        if (!data.TipSucceeded) {
            setTempMessage(p, message);
            }
        else {
            var s1 = $("<span></span>").addClass("text-danger").append(data.ShapeOne);
            var s2 = $("<span></span>").addClass("text-danger").append(data.ShapeTwo);
            var tempPar = $("<p></p>");
            tempPar.append("Opponent picked one of two figures: ")
                .append(s1)
                .append(" or ")
                .append(s2)
                .append(".Try to guess :).");
            setTempMessage(p, tempPar.text());
        }
    }


    function onGameEnded(data) {

        if (data.Error) {
            console.log(data.Error);
            return;
        }

        var game = currentManager.currentGame;
        game.winnerUserName = data.WinnerUserName;

        var header = $("#announcementHeader").find("h3");

        var button = $("#btnViewHistory").prop("disabled", false);
        var journalItself = constructPlayingJournal("journalItself");

        var modal = createBootstrapModalMarkup("modalJournal");
        modal.find(".modal-body").append(journalItself);
        button.on("click", function () {
            console.log(modal.html());
            modal.modal({ backdrop: "static" });
        });

        
        header.text(window.Resources.GameGotAWinner.replace("*",data.WinnerUserName));
    }


    function constructPlayingJournal(journalId) {

        var game = currentManager.currentGame;

        var opponentName;
        
        var container = $("<div></div>").addClass("summary").attr("id", journalId);

        var span = $("<span></span>").css("font-weight", "bold").text("Lobby Owner: ");
        var span2 = $("<span></span>").css("font-weight", "bold").text("Opponent: ");

        var p1 = $("<p></p>").attr("id", "sumLobbyOwnerName").append(span).append(game.lobbyOwnerName);

        var image = new Image();
        image.src = "\../Content/Images/winner-icon-1.png";
        image.style.marginLeft = "10px";

        if (game.winnerUserName === game.lobbyOwnerName)
            p1.append($(image));
        
        container.append(p1);

        var p2 = $("<p></p>").attr("id", "sumOpponentName").append(span2).append(game.opponentName);

        if (game.winnerUserName === game.opponentName)
            p2.append($(image));

        container.append(p2);
        container.css("margin-bottom", "5px");
        
        var table = $("<table></table>").addClass("table table-bordered");
        var thead = $("<thead></thead>").addClass("thead-inverse");

        if (currentManager.isUserLobbyOwner)
            opponentName = game.opponentName;
        else opponentName = game.lobbyOwnerName;

        var headRow = $("<tr></tr>")
            .append($("<th>Round number</th>").attr("rowspan", "2 "))
            .append($("<th></th>").html(game.currentUserName).attr("colspan", "3"))
            .append($("<th></th>").html(opponentName).attr("colspan", "3"));

        table.append(thead);
        thead.append(headRow);

        var subHeadRow = $("<tr></tr>")
            .append($("<th></th>").html("Shape"))
            .append($("<th></th>").html("Using tip"))
            .append($("<th></th>").html("Scores"))
            .append($("<th></th>").html("Scores"))
            .append($("<th></th>").html("Shape"))
            .append($("<th></th>").html("Using tip"));

        thead.append(subHeadRow);

        game.rounds.forEach(function(round) {
            addRowToJournal(round, table);
        });

        container.append(table);
        return container;
    }

    function createBootstrapModalMarkup(modalId) {
        var basic = $("<div></div>").attr("id", modalId)
                                    .attr("role", "dialog")
                                    .attr("aria-hidden", true)
                                    .addClass("modal fade");

        var dialog = $("<div></div>").addClass("modal-dialog");
        basic.append(dialog);

        var content = $("<div></div>").addClass("modal-content");
        dialog.append(content);

        var header = $("<div></div>").addClass("modal-header").append($("<h4></h4>").addClass("text-center").text("Game history"));
        var body = $("<div></div>").addClass("modal-body");
        var footer = $("<div></div>").addClass("modal-footer");

        var cancelButton = $("<button></button>")
            .attr("data-dismiss", "modal")
            .addClass("btn btn-default")
            .text("Cancel");

        footer.append(cancelButton);
        content.append(header).append(body).append(footer);
        //console.log(basic.html());
        return basic;
    }


    function removeBootstrapModalMarkup(modalId) {
        var modal = $("#" + modalId);
        if (modal)
            modal.remove();
    }


    function addRowToJournal(round, table) {

        var length = currentManager.currentGame.rounds.length;

        var tr = $("<tr></tr>");
        tr.append($("<td>").text(round.roundNumber))
            .append($("<td>").text(round.currentUserShape))
            .append($("<td>").text(round.currentUserUsedTip));

        // last round scores are highlighted
        if (round.roundNumber === length)
            tr.append($("<td>").append($("<span></span>").text(round.currentUserScores).css("font-weight", "bold")));
        else tr.append($("<td>").text(round.currentUserScores));

        if (round.roundNumber === length)
            tr.append($("<td>").append($("<span></span>").text(round.opponentScores).css("font-weight", "bold")));
        else tr.append($("<td>").text(round.opponentScores));

        tr.append($("<td>").text(round.opponentShape))
            .append($("<td>").text(round.opponentUsedTip));
        table.append(tr);
    }

    // todo: refactor
    function onAddLobbyMessage(data) {
        var message = data.Message;
        var p = $("#messageLobbies");
        setTempMessage(p, message);
    }


    function onRoundEnded(data) {
        isPlayingState = false;
        var ownerShapeId = data.OwnerShapeId;
        var opponentShapeId = data.OpponentShapeId;
        var winner = data.WinnerUsername;

        var header = $("#announcementHeader h3");

        var mapper = currentManager.shapesIdToNameMapper;
        var imagesMapper = currentManager.imagesIdToShapesIdMapper;

        // set opponent shape here
        // for history
        var round = currentManager.currentGame.rounds[currentManager.currentRoundNumber - 1];

        if (currentManager.isUserLobbyOwner) {
            round.opponentShape = mapper.map[opponentShapeId];
        } else round.opponentShape = mapper.map[ownerShapeId];
        

        var imgSourceUrl;
        var prop;
        if (currentManager.isUserLobbyOwner) {
            for (prop in imagesMapper) {
                if (imagesMapper.hasOwnProperty(prop) && imagesMapper[prop] === opponentShapeId) {
                    imgSourceUrl = $("#" + prop).attr("src");
                    $("#imgOpponentShape").attr("src", imgSourceUrl);
                    break;
                }
            }
        } else {
            for (prop in imagesMapper) {
                if (imagesMapper.hasOwnProperty(prop) && imagesMapper[prop] === ownerShapeId) {
                    imgSourceUrl = $("#" + prop).attr("src");
                    $("#imgOpponentShape").attr("src", imgSourceUrl);
                    break;
                }
            }
        }


        if (winner) {
            setTempMessage(header, window.Resources.RoundGotAWinner.replace("*", winner));
            updateScores(winner);
            setScoresTable();
        } else {
            setTempMessage(header, window.Resources.DrawnRound);
        }

        // set round scores for journal
        if (currentManager.isUserLobbyOwner) {
            round.currentUserScores = ownerScores;
            round.opponentScores = opponentScores;
        } else {
            round.currentUserScores = opponentScores;
            round.opponentScores = ownerScores;
        }

        clearInterval(playingTimer);
        hideTimer();

        if (ownerScores === VICTORY_THRESHOLD || opponentScores === VICTORY_THRESHOLD) {
            
            // request for game ending is pushed only from one player
            if (ownerScores === VICTORY_THRESHOLD) {
                if (currentManager.isUserLobbyOwner)
                    currentManager.endGame(currentManager.currentGame.gameId, ownerScores, opponentScores);
            }

            if (opponentScores === VICTORY_THRESHOLD) {
                if (!currentManager.isUserLobbyOwner) {
                    currentManager.endGame(currentManager.currentGame.gameId, ownerScores, opponentScores);
                }
            }
            return;
        }

        setTimeout(function() {
            currentManager.startPlayingIteration(++currentManager.currentRoundNumber);
        }, 2000);
    }

    function updateScores(winner) {
        if (currentManager.isUserLobbyOwner) {
            if (winner === currentManager.currentGame.currentUserName) {
                ownerScores++;
            } else {
                opponentScores++;
            }

        } else {
            if (winner === currentManager.currentGame.currentUserName) {
                opponentScores++;
            } else ownerScores++;
        }

    }

    function onShapeWasSent(gameId, roundNumber, shapeId, error) {

        if (error) {
            console.log(error);
            return;
        }

        var manager = window.GameManager;
        var game = manager.currentGame;
        var round = game.rounds[roundNumber - 1];

        var mapper = manager.shapesIdToNameMapper;
        var imagesMapper = manager.imagesIdToShapesIdMapper;
        round.shapeWasSent = true;
        round.currentUserShape = mapper.map[shapeId];

        var imgSourceUrl;

        for (var prop in imagesMapper) {
            if (imagesMapper[prop] === shapeId) {
                imgSourceUrl = $("#" + prop).attr("src");
                $("#imgYourShape").attr("src", imgSourceUrl);
                break;
            }
        }
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
        var visible = $("#playingModal").is(":visible");
        if (visible) {
            var p = $("#gameMessage");
            setTempMessage(p, data.Message);
            setTimeout(function() {
                $("#playingModal").modal("hide");
            }, 3000);
        }
    }

    function onLobbyOwnerLeftTheGame(data) {
        var visible = $("#playingModal").is(":visible");
        if (visible) {
            var p = $("#gameMessage");
            setTempMessage(p, data.Message);
            setTimeout(function() {
                $("#playingModal").modal("hide");
            }, 3000);
        }
    }

    // just ui
    // lobbyname should be checked
    function onGameStarted(data) {

        if (data.Error) {
            console.log(data.Error);
            return;
        }


        currentManager.tipWasUsed = false;
        var game = currentManager.currentGame;


        game.gameId = data.GameId;
        game.lobbyOwnerName = data.LobbyOwnerName;
        game.opponentName = data.OpponentName;
        game.lobbyName = data.LobbyName;
        game.rounds = [];

        $("#tipsCount").text(tipsCount);
        $("#playingModal").modal({ backdrop: "static" });
        $("#playingModal").data("lobbyname", data.LobbyName);

        var btnHistory = $("#btnViewHistory");
        btnHistory.prop("disabled", true);  
        btnHistory.off();
        
        removeBootstrapModalMarkup("modalJournal");
       
        $(".yourScoresLegend").first().html(game.currentUserName);

        if (game.currentUserName === data.OpponentName) {
            $(".opponentScoresLegend").first().html(data.LobbyOwnerName);
        } else $(".opponentScoresLegend").first().html(data.OpponentName);

        resetScores();
        setScoresTable();
        currentManager.startPlayingIteration(1);
    }

    function onLobbyWasJoinedAll(data) {

        var opName = data.OpponentName;
        var lobbyId = data.LobbyId;

        var row = $("#tableLobbies").find("tr#" + lobbyId);
        var td = row.find("td").get(3);
        $(td).html(opName);
    }

    // callback when trying to join lobby
    function onLobbyJoined(data) {

        

        if (data.Error) {
            console.log(data.Error);
            return;
        }

        // enable button for joining lobby
        currentManager.isUserJoinedLobby = true;
        $("#btnLeaveJoinedLobby").prop("disabled", !currentManager.isUserJoinedLobby);

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
        setTempMessage(par, message, 2);
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

    function onCorrectLobbyOwning() {
        var manager = window.GameManager;
        manager.isUserLobbyOwner = false;
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
            if (currentManager.currentGame.lobbyName === lobby.lobbyName) {
                currentManager.currentGame.gameState = window.Resources.LobbyStateSummarizing;
            }
        }

        if (lobby.LobbyState === window.Resources.LobbyStateBlocked) {
            $(td).addClass("text-danger");
        }

        $("#joinLobbyModal").modal("hide");
    }
}

// global functions
function getDate(backendFormat) {
    console.log(backendFormat);
    if (!backendFormat) return null;
    var parts = backendFormat.split(" ");
    var date = parts[0];
    var time = parts[1];

    var dateParts = date.split(".");
    var day = dateParts[0];
    var month = dateParts[1];
    var year = dateParts[2];

    var timeParts = time.split(":");

    var hours = timeParts[0];
    var minutes = timeParts[1];
    var seconds = timeParts[2];
    return new Date(year, parseInt(month) - 1, day, hours, minutes, seconds, 0);
}

function LobbiesStorage() {
    this.store = {};
    this.addLobby = function(lobby) {
        if (!this.isLobbyExist(lobby))
            this.store[lobby.LobbyName] = lobby;
    };
    this.isLobbyExist = function(lobby) {
        return !!this.store[lobby.LobbyName];
    };
    this.removeLobby = function(lobby) {
        if (isLobbyExist(lobby))
            delete this.store[lobby.LobbyName];
    };
    this.constructor = LobbiesStorage;
}


function initBlocking() {
    var manager = window.GameManager;
    var date = new Date();
    var btime = manager.blockingTime;
    var span = parseInt((date - btime) / 1000);


    if (manager.userBlocked) {
        if (span < 5) { //60
            console.log("begin to block ui");
            $("#btnJoinToLobby").prop("disabled", true);
            $("#btnPlay").prop("disabled", true);
            $("#btnCreateLobby").prop("disabled", true);
            $("#btnLeaveJoinedLobby").prop("disabled", true);
            beginCountDownToUnblock(5 - span);
        } else {
            manager.unblockUser();
        }
    }
}


function beginCountDownToUnblock(span) {
    var manager = window.GameManager;
    var counter = span;
    var indicator = $("#blockingTimer");
    var timer = setInterval(function() {
        if (counter === 0) {
            clearInterval(timer);
            manager.userBlocked = false;
            manager.BlockingTime = null;
            manager.unblockUser();
            // clear text to some indicator
            indicator.text(null);
            return;
        }
        // update remaining time to some indicator
        // here
        indicator.text(window.Resources.YourBlockingExpiresIn.replace("*", counter));
        counter--;
    }, 1000);
}

function initImagesHandlers() {

    var manager = this.GameManager;
    var currentGame = manager.currentGame;


    Array.from($(".imagesList .thumbnail")).forEach(function(th) {

        $(th).on("click", function() {
            var current = $(this);
            var currentImageId = current.find("img").attr("id");

            var caption = current.find(".caption").toggleClass("hidden").toggleClass("visible");
            if (caption.hasClass("visible")) {
                var others = $(".imagesList img").filter(function() {
                    return $(this).attr("id") !== currentImageId;
                });

                [].slice.call(others).forEach(function(image) {
                    $(image).parent().find(".caption").removeClass("visible").addClass("hidden");
                });

                var rnumber = manager.currentRoundNumber;
                var shapeId = manager.imagesIdToShapesIdMapper[currentImageId];
                manager.sendShape(currentGame.gameId, rnumber, shapeId);
            }
        });
    });
}

function initHandlers() {
    var manager = window.GameManager;
    $("#btnJoinToLobby").prop("disabled", true);
    var btnLeaveLobby = $("#btnLeaveJoinedLobby");


    btnLeaveLobby.prop("disabled", true);

    btnLeaveLobby.click(function() {
        manager.leaveLobby();
    });


    var table = $("#tableLobbies");
    table.on("click", ".clickable-row", function(event) {
        
        var current = $(this);
        current.toggleClass("active").siblings().removeClass("active");
        var lobbyStatus = current.find("td")[2].innerText;
        var lobbyOwner = current.find("td")[1].innerText;
        var button = $("#btnJoinToLobby");

        if (manager.userBlocked || !current.hasClass("active") || lobbyStatus !== window.Resources.LobbyStateAwaitingForPlayers || lobbyOwner === window.UserName)
        {
             button.prop("disabled", true);
        }
        else button.prop("disabled", false);

    });
}

//todo: refactor
function initGameStartup() {
    $("#btnPlay").click(function() {
        var manager = window.GameManager;
        manager.isUserLobbyOwner = true;
        var game = manager.currentGame;

        //todo: just hide button start game if user has no lobby
        if (game.LobbyOwnerName !== game.currentUserName) return;
        manager.startGame();
    });
}


function addUserToTable(table, user) {
    var row = $("<tr>");
    // add icon too
    row.append($("<td>").text(user.UserName));
    row.attr("data-email", user.UserEmail);
    table.append(row);
}

function addLobbyToTable(table, lobby) {
    var manager = window.GameManager;
    var currentGame = manager.currentGame;
    var currentUserName = window.UserName;

    if (lobby.LobbyOwner === currentUserName) {
        currentGame.LobbyOwnerName = currentUserName;
    }

    if (currentUserName === lobby.OpponentName) {
        manager.isUserJoinedLobby = true;
        manager.currentGame.LobbyName = lobby.LobbyName;
        manager.currentGame.LobbyOwnerName = lobby.LobbyOwner;
        $("#btnLeaveJoinedLobby").prop("disabled", !manager.isUserJoinedLobby);
    }

    var row = $("<tr>").addClass("clickable-row").attr("id", lobby.LobbyId);
    row.append($("<td>").text(lobby.LobbyName));
    row.append($("<td>").text(lobby.LobbyOwner));

    if (lobby.LobbyState === window.Resources.LobbyStateBlocked) {
        row.append($("<td>").addClass("text-danger").text(lobby.LobbyState));
    } else row.append($("<td>").text(lobby.LobbyState));

    row.append($("<td>").text(lobby.OpponentName));
    table.append(row);
}


function setTempMessage(paragraph, message, interval)
{
    interval = interval || 3;
    paragraph.text(message);
    var counter = 0;

    var timer = setInterval(function() {
        if (counter++ == interval) {
            paragraph.text(null);
            clearInterval(timer);
        };
    }, 1000);
}


function initLobbyJoin(buttonId) {
    var button = $("#" + buttonId);
    button.on("click", function() {
        var row = $("#tableLobbies tr.active");
        if (!row.length) return;
        var lobbyName = row.find("td")[0].innerText;

        var con = $.connection;
        var hub = $.connection.hub;

        if (con.state === $.signalR.connectionState.disconnected) {
            hub.start().done(function() {
                window.gamesHub.server.joinLobby(lobbyName);
            }).fail(function() {
                console.log("SignalR failed connect to server.");
            });
        } else {
            window.gamesHub.server.joinLobby(lobbyName);
        }
    });
}

function initPlayingWorkflow() {

    var manager = window.GameManager;

    $("#btnLeaveLobby").click(function() {
        manager.leaveLobby();
    });

    $("#btnUseTip").click(function() {
        manager.useTip();
    });
}

function initValidation(buttonId, lobbyStorage) {
    var button = $("#" + buttonId);
    var span = $("#lobbyError");
    var userName = window.UserName;

    $("#modalWindow").on("hidden.bs.modal", function() {
        span.text(null);
        var input = $("#lobbyName");
        input.parent(".form-group").removeClass("has-error").removeClass("has-success");
        //input.parent().find(".glyphicon").removeClass("glyphicon-remove").removeClass("glyphicon-ok");
        $("form-group.has-error").removeClass("has-error");
    });

    button.on("click", function() {
        var input = $("#lobbyName");
        var val = input.val();

        span.text(null);
        input.removeClass("has-error");

        if (!val) {
            span.text("Lobby name must be specified");
            input.parent(".form-group").addClass("has-error");
            //input.parent().find(".glyphicon").addClass("glyphicon-remove");
            return;
        };

        var exist = lobbyStorage.isLobbyExist({ UserName: userName, LobbyName: val });
        if (exist) {
            input.parent(".form-group").addClass("has-error");
            span.text("Lobby with such name is already exist");
            //input.parent().find(".glyphicon").addClass("glyphicon-remove");
            return;
        };
        input.parent(".form-group").addClass("has-success");
        window.gamesHub.server.createLobby(input.val());
    });
}