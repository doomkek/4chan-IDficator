// ==UserScript==
// @name        IDficator
// @namespace   Violentmonkey Scripts
// @match       https://boards.4chan.org/vg/thread/463437991*
// @grant       none
// @version     1.0
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
// @description 1/28/2024, 3:30:15 PM
// ==/UserScript==


(function () {
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
        return extractedPart;
    }

    function getPostId(id) { return "pi" + id.substring(3); }

    function getShitposts(threadId) {
        const url = `https://giggers.moe/getShitposts/${threadId}`;

        fetch(url, {
            method: 'GET',
            headers: { 'Content-Type': 'application/json' },
        }).then(response => {
            if (!response.ok) {
                throw new Error(`HTTP error! Status: ${response.status}`);
            }
            return response.json();
        }).then(data => { applyShitposts(data); })
            .catch(error => { console.error('Error:', error); });
    }

    function addPost(threadId, postId) {
        const url = `https://giggers.moe/addPost?threadId=${threadId}&postId=${postId}`;

        fetch(url, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
        })
            .then(response => {
                if (!response.ok) {
                    throw new Error(`HTTP error! Status: ${response.status}`);
                }
                return response.json();
            })
            .then(data => {
                console.log('Success:', data);
            })
            .catch(error => {
                console.error('Error:', error);
            });
    }

    // event handlers for 4chan-x
    document.addEventListener('ThreadUpdate', function (e) {
        if (e.detail.newPosts.length > 0)
            getShitposts(getThreadId());
    });
    document.addEventListener('QRPostSuccessful', function (e) { addPost(e.detail.threadID, e.detail.postID); });

    // event handlers for default 4chan script
    document.addEventListener('4chanThreadUpdated', function (e) { getShitposts(getThreadId()); });
    document.addEventListener('4chanQRPostSuccess', function (e) { addPost(e.detail.threadId, e.detail.postId); });

    getShitposts(getThreadId());
})();