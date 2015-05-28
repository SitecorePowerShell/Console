/*
-- BSD Licence --
Copyright (c) 2007, Dan Johansson  - All rights reserved.

Redistribution and use in source and binary forms, with or without 
modification, are permitted provided that the following conditions are met:

* Redistributions of source code must retain the above copyright notice,
  this list of conditions and the following disclaimer.
* Redistributions in binary form must reproduce the above copyright notice, 
  this list of conditions and the following disclaimer in the documentation
  and/or other materials provided with the distribution.
* Neither the name of the Dan Johansson nor the names of its contributors
  may be used to endorse or promote products derived from this software
  without specific prior written permission.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS
"AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT
LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR
A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR
CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL,
EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO,
PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR
PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF
LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING
NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
*/
/*
Progressbar class code by Dan Johansson.
jQuery widget & Progressbar enhancements by ManInBlack aka sompylasar.

Copyright (c) 2009, ManInBlack ( maninblack.msk@hotmail.com )  - All rights reserved.
*/

(function($) {
    /*
	 * progressbar class
	 */
    // Consutructor
    function VistaProgressBar(element, options) {
        if (!element) return;

        this._pnode = element;
        this._options = options;
        this._active = false;
        this._initialized = false;
        this._progress = 0;
        this._highlightpos = -120;
        this.mode = (options.mode) ? (options.mode) : "determinate";
        this.width = (options.width && !isNaN(parseInt(options.width))) ? parseInt(options.width) : 250;
        this.highlight = (options.highlight || options.mode == "indeterminate") ? true : false;
        this.highlightspeed = (options.highlightspeed) ? (options.highlightspeed) : 1000;
        this.smooth = (options.smooth) ? true : false;
        this._smoothtarget = this._progress;
        this._smoothdeltapercent = (options.smoothdelta && !isNaN(parseInt(options.smoothdelta))) ? parseInt(options.smoothdelta) : 1;
        this._smoothsteps = (options.smoothsteps && !isNaN(parseInt(options.smoothsteps))) ? parseInt(options.smoothsteps) : 0;
        this._smoothdelay = Math.max(1, (options.smoothdelay && !isNaN(parseInt(options.smoothdelay))) ? parseInt(options.smoothdelay) : 25);
        this._smoothexponent = (this._smoothsteps > 0) ? true : false;

        this._init();
    }

// Private - Creates HTML nodes for progress bar
    VistaProgressBar.prototype._init = function() {
        // return if already initialized
        if (this._initialized) return;

        // build progress bar
        var b = document.createElement("div");
        b.className = "progressbar_bg_middle";

        var br = document.createElement("div");
        br.className = "progressbar_bg_right";

        var bl = document.createElement("div");
        bl.className = "progressbar_bg_left";

        if (this.mode == "determinate") {

            this._wnode = document.createElement("div");
            this._wnode.className = "progressbar_fg";

            this._fnode = document.createElement("div");
            this._fnode.className = "progressbar_fg_right";

            var fl = document.createElement("div");
            fl.className = "progressbar_fg_left";

            if (this.highlight) {
                this._hnode = document.createElement("div");
                this._hnode.className = "progressbar_highlight";
                fl.appendChild(this._hnode);
            }

            this._fnode.appendChild(fl);
            this._wnode.appendChild(this._fnode);
            bl.appendChild(this._wnode);
        } else {

            this._hnode = document.createElement("div");
            this._hnode.className = "progressbar_highlight_ind";
            bl.appendChild(this._hnode);
        }
        br.appendChild(bl);
        b.appendChild(br);

        this._pnode.style.width = this.width + "px";
        this._pnode.appendChild(b);
        this._initialized = true;
    }; // private - Activates highlight
    VistaProgressBar.prototype._activate = function() {
        if (this._active)
            return;
        this._active = true;

        var bind = this;

        if (this.highlight) {
            this._hinterval = window.setInterval(function() { bind._updatehl(bind) }, 50);
        }
    }; // private - highlight interval function
    VistaProgressBar.prototype._updatehl = function(obj) {
        var totalw = 120 + this.width;
        var intervals = Math.floor(this.highlightspeed / 50);
        var delta = Math.floor(totalw / intervals);

        if (this._highlightpos - delta > this.width)
            this._highlightpos = -120;
        else
            this._highlightpos += delta;

        this._hnode.style.marginLeft = this._highlightpos + "px";
    }; // private - Deactivates Highlight
    VistaProgressBar.prototype._deactivate = function() {
        if (!this._active)
            return;

        this._active = false;

        if (this.highlight) {
            window.clearInterval(this._hinterval);
            this._highlightpos = -120;
            this._hnode.style.marginLeft = this._highlightpos + "px";
        }
    }; // public - mode: determinate - adds an integer percentage to progress.
    VistaProgressBar.prototype.addProgress = function(number) {
        this.setProgress(this._progress + number);
    }; // public - mode: determinate - sets progress to a certain integer percentage.
    VistaProgressBar.prototype.setProgress = function(number) {
        if (!this._initialized) return; // return if not ready
        if (this.mode != "determinate") return; // dont use for indeterminate mode
        if (!this._active && this._progress < 100)
            this._activate(); // activate highlight
        this._startprogress = this._progress;
        this._progress = number;
        if (this._progress >= 100) {
            this._progress = 100;
            this._deactivate();
        } else if (this._progress <= 0) {
            this._progress = 0;
            this._deactivate();
        }

        // hide right background due to overlap
        if ((this._progress / 100) * this.width > 40)
            this._fnode.className = "progressbar_fg_right";
        else
            this._fnode.className = "";
        if (this.smooth && this._progress > 0) {
            this._smoothtarget = this._progress;
            if (!this._sinterval && this._startprogress < 100) // dont start a new interval if one is already open.
            {
                this._smoothCalcDelta();
                var bind = this;
                this._sinterval = window.setInterval(function() { bind._smoothProgress() }, this._smoothdelay);
            }
        } else {
            this._wnode.style.width = this._progress + "%";
        }
    }; // private  -  Smooth transistion helper function
    VistaProgressBar.prototype._smoothProgress = function() {
        this._startprogress += Math.min(this._smoothdelta, (this._smoothtarget - this._startprogress));
        this._smoothCalcDelta();
        if (Math.abs(this._smoothdelta) < 0.01 || this._startprogress >= this._smoothtarget || this._startprogress >= 100) {
            window.clearInterval(this._sinterval);
            this._sinterval = false;
            this._startprogress = this._smoothtarget;
        }
        this._wnode.style.width = this._startprogress + "%";
    }; // private  -  Smooth transistion delta helper function
    VistaProgressBar.prototype._smoothCalcDelta = function() {
        this._smoothdelta = (this._smoothexponent && this._smoothsteps > 0
            ? (this._smoothtarget - this._startprogress) / this._smoothsteps
            : this._smoothdeltapercent);
    }; // public - mode: indeterminate - starts animation
    VistaProgressBar.prototype.start = function() {
        if (this.mode != "indeterminate") return;
        this._activate();
    }; // public - mode: indeterminate - stops animation
    VistaProgressBar.prototype.stop = function() {
        if (this.mode != "indeterminate") return;
        this._deactivate();
    };
    $.widget("ui.VistaProgressBar", {
        _init: function() {
            this._min = (this.options.minProgress || 0);
            this.options.minProgress = 0;
            this._max = (this.options.maxProgress || 100);
            this.options.maxProgress = 100;

            this._object = new VistaProgressBar(this.element[0], this.options);

            if (typeof this.options.progress != "undefined")
                this.setProgress(this.options.progress);
        },
        addProgress: function(number) {
            number = 100 * (number - this._min) / (this._max - this._min);
            this._object.addProgress(number);
        },
        setProgress: function(number) {
            number = 100 * (number - this._min) / (this._max - this._min);
            this._object.setProgress(number);
        },
        getProgress: function() {
            var percent = this._object._progress;
            return ((percent / 100) * (this._max - this._min) + this._min);
        },
        resetProgress: function() {
            this.setProgress(this._min);
        },
        start: function() {
            this._object.start();
        },
        stop: function() {
            this._object.stop();
        },
        getMode: function() {
            return this._object.mode;
        }
    });


    $.ui.VistaProgressBar.defaults = {
        mode: "determinate", // or 'indeterminate' for marquee progressbar
        width: 355, // in pixels
        minProgress: 0,
        maxProgress: 100,
        progress: 0,
        highlight: true,
        highlightspeed: 1000,
        smooth: true,
        smoothdelta: 1,
        smoothsteps: 10, // > 0 exponent easing, == 0 linear
        smoothdelay: 25 // in milliseconds
    };

    $.ui.VistaProgressBar.getter = "getProgress";
})(jQuery);