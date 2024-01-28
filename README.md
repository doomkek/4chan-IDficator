This project allow users to bring IDs into boards that does not have them.
Why? Its up to you.

# Disclaimer
This project was done in few hours, its as basic as it gets, currently I just want to see if people are interested in using it.

# Installation
You need to install userscript manager (like Violentmonkey), create new script and copy [script](userscript.js) content. <br>
You can read (or press skip lmao) this script even if you don't know what you are looking at, its very short, and have comments to explain important parts. <br>

It is compatible with 4chan-x. <br>
If you are not using 4chan-x make sure native extension is **NOT** disabled in settings.
![image](https://github.com/doomkek/4chanIdificator/assets/141933494/426662d0-d1b5-4d1e-91ad-50a361ca5e9d)


# Brief description of how it works
- When you load thread, script will make a request to domain `https://giggers.moe/getShitposts/{threadID}` to fetch all existing posts and their IDs. 
- When you make a post, script will make a request to `https://giggers.moe/addPost?threadId=${threadId}&postId=${postId}` to save relation between your postID and ID. 
- User IP address and `threadID` is used to create a hash (ID) that you will see on user post 
![image](https://github.com/doomkek/4chanIdificator/assets/141933494/a51a8427-b099-4e98-b2f9-266622bd7b6f)
Unfortunately I have to hide hash implementation `Utils.GenerateHash(threadId, clientIpAddress);` because if people know how hash is made you can bruteforce real IP address from ID.

# Privacy
Server and Web service on `giggers.moe` does not log IP address, unfortunately there is no way to prove it and you'd have to believe me with this one. <br>
This is how database looks like <br>
![image](https://github.com/doomkek/4chanIdificator/assets/141933494/b573176b-5b93-450c-b1f8-96f7cd86be6b)

Currently DB stores everything indefinetely, later on I will implement scheduled task that will drop old records from dead threads.

