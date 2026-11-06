// Sitecore renders Start Menu Actions in the right column as plain options with no id or
// submenu glyph, so SPE menus opened from them pop up at the cursor. Give them the folder
// markup so they look like Security Tools and the server-built menu anchors to the right.
(function (document) {
    "use strict";

    function upgrade() {
        var menu = document.getElementById("StartMenu");
        if (!menu) {
            return;
        }

        var glyph = menu.querySelector(".scStartMenuRightFolderGlyph, .scStartMenuProgramsGlyph");
        var links = menu.querySelectorAll(".scStartMenuRight a.scStartMenuLeftOption[onclick*='powershell:startmenuaction']");
        for (var i = 0; i < links.length; i++) {
            var link = links[i];
            var icon = link.querySelector(".scStartMenuLeftOptionIcon");
            var name = link.querySelector(".scStartMenuLeftOptionDisplayName");
            if (!name) {
                continue;
            }

            var label = document.createElement("div");
            label.className = "scStartMenuRightFolderDisplayName";
            label.textContent = name.textContent;

            link.id = "SpeStartMenuAction" + i;
            link.className = "scStartMenuRightFolder";
            link.innerHTML = "";
            if (icon) {
                icon.className = "scStartMenuRightFolderIcon";
                link.appendChild(icon);
            }
            if (glyph) {
                var arrow = glyph.cloneNode(false);
                arrow.className = "scStartMenuRightFolderGlyph";
                link.appendChild(arrow);
            }
            link.appendChild(label);
        }
    }

    if (document.readyState === "loading") {
        document.addEventListener("DOMContentLoaded", upgrade);
    } else {
        upgrade();
    }
})(document);
