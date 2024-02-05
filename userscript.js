// ==UserScript==
// @name        4chan-IDficator
// @namespace   Violentmonkey Scripts
// @match       *://boards.4chan.org/*
// @grant       none
// @version     0.4
// @author      Doomkek
// @include     http://boards.4chan.org/*
// @include     https://boards.4chan.org/*
// @include     http://sys.4chan.org/*
// @include     https://sys.4chan.org/*
// @include     http://www.4chan.org/*
// @include     https://www.4chan.org/*
// @include     http://boards.4channel.org/*
// @include     https://boards.4channel.org/*
// @include     http://sys.4channel.org/*
// @include     https://sys.4channel.org/*
// @include     http://www.4channel.org/*
// @include     https://www.4channel.org/*
// @connect     https://giggers.moe
// @description Allow users to bring IDs into boards that does not have them.
// @license     MIT
// @homepageURL https://github.com/doomkek/4chan-IDficator
// @downloadURL https://update.greasyfork.org/scripts/485980/4chan-IDficator.user.js
// @updateURL   https://update.greasyfork.org/scripts/485980/4chan-IDficator.meta.js
// ==/UserScript==


(function () {
    var SERVICE_URL = 'https://giggers.moe';
    var USE_ID = true;
    var IS_4CHANX = false;

    var POSTS = [];
    var menu;

    function applyShitposts(data) {
        if (data.length == 0)
            return;

        for (var i = 0; i < data.length; i++) {
            var id = "shitpost_" + data[i].postId;
            var shitpost = document.getElementById(id);

            if (shitpost) {
                var index = POSTS.findIndex(p => p.userId == data[i].userHash);

                POSTS[index].posts.push(shitpost);
                changePostHideState(data[i].postId, POSTS[index].state.hidden);

                continue;
            }

            POSTS.push({
                userId: data[i].userHash,
                posts: [shitpost],
                state: { hidden: false }
            });

            applyId(data[i].postId, data[i].userHash);
        }
    }

    function changePostHideState(postId, hide) {
        if (IS_4CHANX) {
            var post = document.getElementById('sa' + postId);
            if (hide && document.getElementById('pc' + postId).querySelector('.stub') == null || !hide && document.getElementById('pc' + postId).querySelector('.stub') != null)
                document.getElementById('sa' + postId).querySelector('.hide-reply-button').click();
        } else {
            var post = document.getElementById('pc' + postId);
            if (hide && !post.classList.contains("post-hidden") || !hide && post.classList.contains("post-hidden"))
                post.classList.toggle("post-hidden");
        }
    }

    function applyId(postId, hash) {
        var post = document.getElementById("pi" + postId).getElementsByClassName('postNum')[0];

        var a = document.createElement('span');
        a.id = "shitpost_" + postId
        a.className = hash;
        a.innerText = hash;
        a.style.backgroundColor = "#" + hash;
        a.style.color = getContrastColor(hash);
        a.style.marginLeft = "4px";
        a.style.cursor = "pointer";

        a.onmouseenter = function (e) {
            var posts = document.getElementsByClassName(hash);
            a.title = posts.length + (posts.length > 1 ? " posts" : " post") + " by this ID";
        };

        a.onmouseleave = function (e) { a.title = ""; };

        a.onclick = function (e) {
            showMenu(e.target);

            if (menu.style.display == "block") {
                menu.style.display = "none";
                document.removeEventListener('click', handleMenuClick);
            } else {
                menu.style.display = "block";
                document.addEventListener('click', handleMenuClick);
            }
        };

        post.insertAdjacentElement('afterend', a);
    }

    function getThreadId() {
        const parsedUrl = new URL(window.location.href);
        const pathSegments = parsedUrl.pathname.split('/');
        const lastPart = pathSegments.filter(segment => segment !== '').pop();
        const extractedPart = lastPart.split('#')[0];
        return { boardId: pathSegments[1], threadId: extractedPart };
    }

    function getContrastColor(hexColor) {
        const hex = hexColor.slice(1);
        const r = parseInt(hex.slice(0, 2), 16);
        const g = parseInt(hex.slice(2, 4), 16);
        const b = parseInt(hex.slice(4, 6), 16);
        const luminance = (0.299 * r + 0.587 * g + 0.114 * b) / 255;
        const textColor = luminance > 0.5 ? '#000000' : '#FFFFFF';

        return textColor;
    }

    function createMenu() {
        if (menu)
            menu.remove();

        menu = document.createElement('div');
        menu.style.width = "80px";
        menu.style.position = "absolute";
        menu.style.boxShadow = "0 1px 2px rgba(0, 0, 0, .15)";
        menu.style.background = IS_4CHANX ? "var(--fcsp-background)" : "#b7c5d9";
        menu.style.color = IS_4CHANX ? "var(--fcsp-text)" : "#000";
        menu.style.border = IS_4CHANX ? "1px solid var(--fcsp-border)" : "1px solid #b7c5d9";
        menu.style.display = "none";
        menu.getId = function () { return menu.id.split('_')[1]; }
        menu.getClass = function () { return menu.id.split('_')[2]; }

        insertMenuItem({
            name: "Highlight", handler: function () {
                var posts = document.getElementsByClassName(menu.getClass());
                for (var i = 0; i < posts.length; i++) {
                    var parentId = posts[i].id.split('_')[1];
                    var parent = document.getElementById('p' + parentId);
                    parent.classList.toggle("highlight");
                }
            }
        });

        //TODO: make is cycle 
        // ignore hidden posts, currently it will move menu to the item but since its hidden view would stay the same    
        insertMenuItem({
            name: "↑ Prev", handler: function () {
                var posts = document.getElementsByClassName(menu.getClass());
                var id = menu.getId();
                for (var i = posts.length - 1; i >= 0; i--) {
                    if (posts[i].id.split('_')[1] < id) {
                        showMenu(posts[i]);
                        posts[i].scrollIntoView({ behavior: 'instant', block: 'center' });
                        break;
                    } else if (i - 1 == 0) {
                        showMenu(posts[posts.length - 1]);
                        posts[posts.length - 1].scrollIntoView({ behavior: 'instant', block: 'center' });
                    }
                }
            }
        });

        insertMenuItem({
            name: "↓ Next", handler: function () {
                var posts = document.getElementsByClassName(menu.getClass());
                var id = menu.getId();
                for (var i = 0; i < posts.length; i++) {
                    if (posts[i].id.split('_')[1] > id) {
                        showMenu(posts[i]);
                        posts[i].scrollIntoView({ behavior: 'instant', block: 'center' });
                        break;
                    } else if (i + 1 == posts.length) {
                        showMenu(posts[0]);
                        posts[0].scrollIntoView({ behavior: 'instant', block: 'center' });
                    }
                }
            }
        });

        var toggleHide = function (hide) {
            var posts = document.getElementsByClassName(menu.getClass());
            for (var i = 0; i < posts.length; i++) {
                var id = posts[i].id.split('_')[1];
                var index = POSTS.findIndex(p => p.userId == menu.getClass());
                POSTS[index].state.hidden = hide;
                changePostHideState(id, hide);
            }
        }

        insertMenuItem({ name: "Hide ID", handler: () => toggleHide(true) });
        insertMenuItem({ name: "Unhide ID", handler: () => toggleHide(false) });
    }

    function showMenu(target) {
        menu.id = "menu_" + target.id.split('_')[1] + "_" + target.innerText;
        menu.style.left = Math.round(target.getBoundingClientRect().left) + "px";
        target.insertAdjacentElement('afterend', menu);
    }

    function handleMenuClick(e) {
        if (!e.target.id.startsWith('shitpost_') && !e.target.id.startsWith('menu_item_') && menu.style.display == "block" && e.target != menu) {
            menu.style.display = "none";
            document.removeEventListener('click', handleMenuClick);
        }
    }

    // {name: "", handler: function }
    function insertMenuItem(item) {
        var menuItem = document.createElement('div');
        menuItem.id = "menu_item_12345";
        menuItem.innerText = item.name;
        menuItem.style.height = IS_4CHANX ? "21px" : "18px";
        menuItem.style.cursor = "pointer";
        menuItem.style.paddingLeft = "4px";
        menuItem.style.color = IS_4CHANX ? "var(--fcsp-text)" : "#000";
        menuItem.style.background = IS_4CHANX ? "var(--fcsp-background)" : "#d6daf0";
        menuItem.style.userSelect = "none";

        menuItem.addEventListener('mouseover', function () { menuItem.style.background = IS_4CHANX ? "var(--fcsp-border)" : "#eef2ff"; });
        menuItem.addEventListener('mouseout', function () { menuItem.style.background = IS_4CHANX ? "var(--fcsp-background)" : "#d6daf0"; });
        menuItem.addEventListener('click', item.handler);

        menu.appendChild(menuItem);
    }

    function getShitposts(boardId, threadId) {
        const url = `${SERVICE_URL}/getShitposts/${boardId}/${threadId}`;

        fetch(url, { method: 'GET', headers: { 'Content-Type': 'application/json' } })
            .then(response => {
                if (!response.ok)
                    throw new Error(`HTTP error! Status: ${response.status}`);
                return response.json();
            }).then(data => { applyShitposts(data); });
    }

    function addPost(boardId, threadId, postId) {
        if (!USE_ID)
            return;

        const url = `${SERVICE_URL}/addPost?threadId=${threadId}&postId=${postId}&boardId=${boardId}`;
        fetch(url, { method: 'POST' });
    }

    document.addEventListener('ThreadUpdate', function (e) {
        if (e.detail.newPosts.length == 0)
            return;

        getShitposts(getThreadId().boardId, getThreadId().threadId);
    });

    document.addEventListener('4chanXInitFinished', function (e) { IS_4CHANX = true; createMenu(); });

    document.addEventListener('4chanThreadUpdated', function (e) { getShitposts(getThreadId().boardId, getThreadId().threadId); });

    document.addEventListener('QRPostSuccessful', function (e) { addPost(e.detail.boardID, e.detail.threadID, e.detail.postID); });
    document.addEventListener('4chanQRPostSuccess', function (e) { addPost(getThreadId().boardId, e.detail.threadId, e.detail.postId); });

    // need for tracking non 4chan-x QR being added to the DOM
    var doom = function (e) {
        if (e.target.id != "quickReply")
            return;

        var cbID = document.createElement('span');
        cbID.id = 'cbID';
        cbID.innerHTML = '<label>[<input type="checkbox" checked="true" name="cbID">Use ID?]</label>';
        cbID.addEventListener("change", function (e) { USE_ID = cbID.querySelector('input').checked; });
        e.target.querySelector('#qrSpoiler').insertAdjacentElement('afterend', cbID);
    };

    document.addEventListener('QRDialogCreation', function (e) {
        var cbID = document.createElement('label');
        cbID.id = 'cbID';
        cbID.innerHTML = '<input type="checkbox" checked="true" name="cbID">Use ID?</input>';
        cbID.addEventListener("change", function (e) { USE_ID = cbID.querySelector('input').checked; });

        e.target.querySelector('.move').appendChild(cbID);
        document.getElementsByTagName('body')[0].removeEventListener('DOMNodeInserted', doom);
    });

    document.getElementsByTagName('body')[0].addEventListener("DOMNodeInserted", doom);

    createMenu();
    getShitposts(getThreadId().boardId, getThreadId().threadId);
})();