function startPlayingTimer(startValue) {
    var val = startValue;
    var table = $("#playingTimer");
    var timer = setInterval(function() {
        updateTimerValue(table, val);
        if (val === 0) {
            clearInterval(timer);
            return;
        }
        val--;
    }, 1000);
}

function startCountDownTimer(startValue) {
    var val = startValue;
    var elTimer = $("#announcementHeader");
    var timer = setInterval(function () {
        updateTimerValue(val, elTimer);
        if (val === 0) {
            clearInterval(timer);
            return;
        }
        val--;
    }, 1000);
}

function updateTimerValue(value, elementTimer) {
    
}


function beginRound(roundNumber) {
    
}