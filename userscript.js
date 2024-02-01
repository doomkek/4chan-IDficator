// ==UserScript==
// @name        4chan-IDficator
// @namespace   Violentmonkey Scripts
// @match       *://boards.4chan.org/*
// @grant       none
// @version     0.3
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

    function applyShitposts(data) {
        if (data.length == 0)
            return;

        for (var i = 0; i < data.length; i++) {
            var id = "shitpost_" + data[i].postId;
            var shitpost = document.getElementById(id);

            if (shitpost)
                continue;

            applyId(data[i].postId, data[i].userHash);
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
            var posts = document.getElementsByClassName(e.target.className);
            for (var i = 0; i < posts.length; i++) {
                var parentId = posts[i].id.split('_')[1];
                var parent = document.getElementById('p' + parentId);
                parent.classList.toggle("highlight");
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
    document.addEventListener('4chanThreadUpdated', function (e) { getShitposts(getThreadId().boardId, getThreadId().threadId); });

    document.addEventListener('QRPostSuccessful', function (e) { addPost(e.detail.boardID, e.detail.threadID, e.detail.postID); });
    document.addEventListener('4chanQRPostSuccess', function (e) { addPost(e.detail.boardId, e.detail.threadId, e.detail.postId); });

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

    getShitposts(getThreadId().boardId, getThreadId().threadId);
})();