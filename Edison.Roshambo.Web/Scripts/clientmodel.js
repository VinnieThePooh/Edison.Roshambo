function GameManager(gamesHub) {
    var current = this;
    this.gamesHub = gamesHub;
    this.currentLobbyRoundDuration = 5;
    this.beforeRoundsTimeout = 3;
    this.isUserLobbyOwner = false;
    this.currentUserScores = 0;
    this.opponentScores = 0;
    this.currentRoundNumber = 0; // for tracing gaming process
    this.tipWasUsed = false;
    this.userReloadedPage = false;
    this.userBlocked = false;
    this.blockingTime = new Date();
    this.currentGame = { gameId: "", lobbyName: "", currentUserName: "", opponentName: "", rounds: [], gameState: "", lobbyOwnerName:"" }
    this.sendShape = function () {
        // shapeNameOrValue, roundNumber, gameId
        // clearTimerCountDown
    };
    this.userTip = function () {

        // round props: roundNumber, GameId,
        // if (tip was not used)
        //  sendRequest to Server
        // if (opponent did send shape)
        // actually use tip - return result. 
        //otherwise  - no
        // tip burns anyway
    }

    this.unblockUser = function () {
        var manager = this;
        var hub = $.connection.hub;
        if (hub.state === $.signalR.connectionState.disconnected) {
            hub.start().done(function () {
                manager.gamesHub.server.unblockUser(manager.currentGame.currentUserName);
            }).fail(function() {
                console.log("could not connect to server");
            });
        } else {
            manager.gamesHub.server.unblockUser(manager.currentGame.currentUserName);
        }
    }

    this.gameResults = { winnerUserName: "", winnerScores: 0, opponentScores: 0 }
    this.startGame = function () {
        // use serverside method to start game
        // ss must return: gameId, lobbyOwnerName, opponentName
        // set this values to currentGame properties 
        var server = gamesHub.server;
        server.startGame(this.currentGame.opponentName);
    }

    this.startRound = function (roundNumber) {
        //this method is automatically called after previous round completed
        // logic: 1) before game countDown,  2)whileGameCountDown
        // For 2) User sends his choice or user loses the round
    }

    
    this.endRound = function(gameId, roundNumber, shapeChosen) {
        
    }


    this.leaveGame = function() {
        var server = this.gamesHub.server;
        var lobbyName = this.currentGame.LobbyName;
        server.leaveLobby(lobbyName);
        $("#playingModal").modal("hide");
    }


    this.startPlayingIteration = function (roundNumber) {
        var game = this.currentGame;
        var round = {
            roundNumber: roundNumber,
            opponentShape: "",
            currentUserShape: "",
            opponentUsedTip: false,
            currentUserUsedTip: false
        };
        game.rounds.push(round);
        startBeforeGameCountDown(roundNumber);
    }
    
    function hideShapes() {
        $("div.yourShape").css("display", "none");
        $("div.opponentShape").css("display", "none");
    }

    function showShapes() {
        var div = $("div.yourShape").first();
        div.css("display", "block");

        var img = div.find("img");
        // set default image

        div = $("div.opponentShape").first();
        div.css("display", "block");
        img = div.find("img");
        //set default image
    }

    function hideTimer() {
        var timer = $("#tableTimer tr.time p").first();
        timer.css("display", "none");
        var tinfo = $("#tableTimer p.text-info").first();
        tinfo.css("display", "none");
    }

    function showTimer() {
        var timer = $("#tableTimer tr.time p").first();
        timer.text(null);
        timer.css("display", "block");
        var tinfo = $("#tableTimer p.text-info").first();
        tinfo.css("display", "block");
    }


    function startBeforeGameCountDown(roundNumber) {
        hideTimer();
        hideShapes();
        var header = $("#announcementHeader h3");
        var message = window.Resources.NextRoundWillStartIn.replace("*", roundNumber);
        var timeout = current.beforeRoundsTimeout;

        var timer = setInterval(function () {
            if (timeout === 0) {
                header.text(null);
                clearInterval(timer);
                header.text("FIGHT!!!");
                // begin round timeout
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
        var timeInfo = $("#tableTimer tr.time p").first();
        current.currentRoundNumber = roundNumber;
        // fix round on server

        var duration = current.currentLobbyRoundDuration;
        var counter = 0;
        showTimer();
        showShapes();

        var timer = setInterval(function () {
            timeInfo.text(duration);
            // clear fight message
            if (counter === 1) {
                header.text(null);
            }

            if (duration === 0) {
                clearInterval(timer);
                hideTimer();
                return;
                // end round here
                // if had no choice - send lose result
            }
            duration--;
            counter++;
        }, 1000);
    }
}


function getDate(backendFormat) {
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
    debugger;
    return new Date(year, parseInt(month)-1 ,day, hours, minutes, seconds, 0);
}

function LobbiesStorage() {
    this.store = {};
    this.addLobby= function(lobby)
    {
        if (!this.isLobbyExist(lobby))
        this.store[lobby.LobbyName] = lobby;
    }

    this.isLobbyExist = function(lobby) {
        return !!this.store[lobby.LobbyName];
    }

    this.removeLobby = function(lobby) {
        if (isLobbyExist(lobby))
            delete this.store[lobby.LobbyName];
    }
    this.constructor = LobbiesStorage;
}


function initBlocking() {
    var manager = window.GameManager;
    var date = new Date();
    var btime = manager.blockingTime;
    var span = parseInt((date - btime)/1000);


    if (manager.userBlocked) {
        if (span < 5) { //60
            console.log("begin to block ui");
            $("#btnJoinToLobby").prop("disabled", true);
            $("#btnPlay").prop("disabled", true);
            $("#btnCreateLobby").prop("disabled", true);
            $("#btnLeaveJoinedLobby").prop("disabled", true);
            beginCountDownToUnblock(5 -span);
        } else {
            manager.unblockUser();
        }
    }
}


function beginCountDownToUnblock(span) {
    var manager = window.GameManager;
    var counter = span;
    var indicator = $("#blockingTimer");
    var timer = setInterval(function () {
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
        indicator.text(window.Resources.YourBlockingExpiresIn.replace("*",counter));
        counter--;
    }, 1000);
}

function initImagesHandlers() {

    var img1Clicks = 0, img2Clicks = 0, img3Clicks = 0, img4Clicks = 0, img5Clicks = 0;
    var t1, t2, t3, t4, t5;
    var delay = 300;
    Array.from($(".imagesList .thumbnail")).forEach(function (th) {
    
        $(th).on("click", function () {
            var current = $(this);
            var currentImageId = $(this).find("img").attr("id");
            switch(currentImageId) {
                case "img1":
                    img1Clicks++;
                    if (img1Clicks === 1) {
                        t1 = setTimeout(function() {
                            img1Clicks = 0;
                            var caption = current.find(".caption").toggleClass("hidden").toggleClass("visible");
                            if (caption.hasClass("visible")) {
                               var others = $(".imagesList img").filter(function(index) {
                                    return $(this).attr("id") !== currentImageId;
                               });

                                [].slice.call(others).forEach(function(image) {
                                    $(image).parent().find(".caption").removeClass("visible").addClass("hidden");
                                });
                            }
                        }, delay);
                    } else {
                        clearTimeout(t1);
                        img1Clicks = 0;
                    }
                    break;
                case "img2":
                    img2Clicks++;
                    if (img2Clicks === 1) {
                        t2 = setTimeout(function () {
                            img2Clicks = 0;
                            var caption = current.find(".caption").toggleClass("hidden").toggleClass("visible");
                            if (caption.hasClass("visible")) {
                                var others = $(".imagesList img").filter(function (index) {
                                    return $(this).attr("id") !== currentImageId;
                                });

                                [].slice.call(others).forEach(function (image) {
                                    $(image).parent().find(".caption").removeClass("visible").addClass("hidden");
                                });
                            }
                        }, delay);
                    } else {
                        clearTimeout(t2);
                        img2Clicks = 0;
                    }
                    break;
                case "img3":
                    img3Clicks++;
                    if (img3Clicks === 1) {
                        t3 = setTimeout(function () {
                            img3Clicks = 0;
                            var caption = current.find(".caption").toggleClass("hidden").toggleClass("visible");
                            if (caption.hasClass("visible")) {
                                var others = $(".imagesList img").filter(function (index) {
                                    return $(this).attr("id") !== currentImageId;
                                });

                                [].slice.call(others).forEach(function (image) {
                                    $(image).parent().find(".caption").removeClass("visible").addClass("hidden");
                                });
                            }
                        }, delay);
                    } else {
                        clearTimeout(t3);
                        img3Clicks = 0;
                    }
                    break;
                case "img4":
                    img4Clicks++;
                    if (img4Clicks === 1) {
                        t4 = setTimeout(function () {
                            var caption = current.find(".caption").toggleClass("hidden").toggleClass("visible");
                            if (caption.hasClass("visible")) {
                                var others = $(".imagesList img").filter(function (index) {
                                    return $(this).attr("id") !== currentImageId;
                                });

                                [].slice.call(others).forEach(function (image) {
                                    $(image).parent().find(".caption").removeClass("visible").addClass("hidden");
                                });
                            }
                        }, delay);
                    } else {
                        clearTimeout(t4);
                        img4Clicks = 0;
                    }
                    break;
                case "img5":
                    img5Clicks++;
                    if (img5Clicks === 1) {
                        t5 = setTimeout(function () {
                            img5Clicks = 0;
                            var caption = current.find(".caption").toggleClass("hidden").toggleClass("visible");
                            if (caption.hasClass("visible")) {
                                var others = $(".imagesList img").filter(function (index) {
                                    return $(this).attr("id") !== currentImageId;
                                });

                                [].slice.call(others).forEach(function (image) {
                                    $(image).parent().find(".caption").removeClass("visible").addClass("hidden");
                                });
                            }
                        }, delay);
                    } else {
                        clearTimeout(t5);
                        img5Clicks = 0;
                    }
                    break;
            }
            
        }).on("dblclick", function(e) {
            e.preventDefault();
        });


//        $(img).on("dblclick", function() {
//            var current = $(this);
//            console.log("Was clicked: " + current.attr("src"));
//        });
    });
}

function initHandlers() {
    $("#btnJoinToLobby").prop("disabled", true);
    $(" #btnLeaveJoinedLobby").prop("disabled", true);

    var table = $("#tableLobbies");
    var manager = window.GameManager;
    table.on("click", ".clickable-row", function (event) {
        var current = $(this);
        current.toggleClass("active").siblings().removeClass('active');
        var lobbyStatus = current.find("td")[2].innerText;
        var lobbyOwner = current.find("td")[1].innerText;
        var button = $("#btnJoinToLobby");
        if (manager.userBlocked || !current.hasClass("active") || lobbyStatus != window.Resources.LobbyStateAwaitingForPlayers || lobbyOwner == window.UserName) {
            button.prop("disabled", true);
        } else button.prop("disabled", false);

    });
}

//todo: refactor
function initGameStartup() {
    $("#btnPlay").click(function () {
        var manager = window.GameManager;
        var game = manager.currentGame;

        //todo: just hide button start game if user has no lobby
        if (game.LobbyOwnerName !== game.currentUserName) return;
        manager.startGame();
    });
}

function initLobbyJoin(buttonId) {
    var button = $("#" + buttonId);
    var par = $("#messageLobbies");
    button.on("click", function() {
        var row = $("#tableLobbies tr.active");
        if (!row.length) return;
        var lobbyName = row.find("td")[0].innerText;
        window.gamesHub.server.joinLobby(lobbyName);
    });
}

function initPlayingWorkflow() {

    var manager = window.GameManager;
    
    $("#btnLeaveLobby").click(function () {
        manager.leaveGame();
    });
}

function initValidation(buttonId, lobbyStorage) {
    var button = $("#" + buttonId);
    var span = $("#lobbyError");
    var userName = window.UserName;

    $('#modalWindow').on('hidden.bs.modal', function() {
        span.text(null);
        var input = $("#lobbyName");
        input.parent(".form-group").removeClass("has-error").removeClass("has-success");
        //input.parent().find(".glyphicon").removeClass("glyphicon-remove").removeClass("glyphicon-ok");
        $("form-group.has-error").removeClass("has-error");
    });

    button.on("click", function () {
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

