# Publishing Gravitiria

Two guides, both written for someone starting with nothing: no accounts, no tools installed, just the project folder.

- Part A puts the source code on GitHub.
- Part B puts a playable browser version on Unity Play.
- Part C covers release downloads (Windows zip, Android APK) on GitHub.

Steps that depend on a button label are written with the label as it appears today; vendors rename things, so if a label is missing, look for the nearest equivalent.

---

## Part A: Source code on GitHub

### A1. What goes up, what stays home

Unity regenerates a lot of files. Only these belong in the repository:

```
Assets/   Packages/   ProjectSettings/   Tools/   docs/
README.md   README.ja.md   CHANGELOG.md   LICENSE   .gitignore   .gitattributes   .vsconfig
```

Never upload `Library/`, `Temp/`, `Logs/`, `obj/`, `UserSettings/`, `.utmp/` or any `Build/` folder. The included `.gitignore` already lists them, so Git will skip them, but delete them from the folder you upload anyway. It keeps the first commit honest and the folder small (about 50 MB on disk, 30 MB zipped).

Check for large files before you start. GitHub rejects any single file over 100 MB. The largest file in this project is the stage music at under 10 MB, so nothing special is needed.

### A2. Create the GitHub account

1. Open https://github.com/signup in a browser.
2. Enter an email address, choose a password and a username. The username appears in the repository address (`github.com/<username>/gravitiria`), so pick something you are happy to show.
3. Complete the verification puzzle and enter the code that arrives by email.
4. Choose the Free plan. Skip the personalisation questions if offered.

### A3. Install GitHub Desktop and sign in

Git has a command line, but GitHub Desktop is enough for this and easier to explain.

1. Download it from https://desktop.github.com and run the installer. It opens by itself when done.
2. On the welcome screen choose **Sign in to GitHub.com**. A browser window asks you to authorise GitHub Desktop; click **Authorize desktop**.
3. Back in GitHub Desktop, confirm the name and email that will be attached to your commits, then click **Finish**. These are public on every commit, so use the name you want to be known by.

### A4. Turn the project folder into a repository

1. Place the cleaned project folder somewhere permanent, for example `C:\Projects\Gravitiria`. Moving it later confuses the app.
2. In GitHub Desktop: **File > Add local repository...**, click **Choose...**, select the folder, click **Add repository**.
3. The app says the folder "does not appear to be a Git repository" and offers a link **create a repository here instead**. Click it.
4. In the **Create a New Repository** dialog:
   - **Name**: `Gravitiria` (this becomes the folder name on GitHub; keep it).
   - **Description**: *A first-person gravity puzzle made with Unity.*
   - **Git ignore**: leave as **None**. The project already has a Unity `.gitignore`.
   - **License**: leave as **None**. The project already has `LICENSE` (MIT).
   - Leave **Initialize this repository with a README** unchecked; the README exists.
   - Click **Create repository**.
5. The left pane now lists every file as a change. Scroll through it once. You should see `Assets`, `Packages`, `ProjectSettings`, `Tools`, `docs` and the top-level files, and you should **not** see `Library`. If `Library` is there, stop: the `.gitignore` is missing or misnamed.
6. At the bottom left, type a summary: `Initial import of Gravitiria`. Click **Commit to main**.

### A5. Publish

1. Click **Publish repository** in the toolbar.
2. In the dialog, keep **Name** as `Gravitiria`, paste the description, and decide on **Keep this code private**. Untick it to make the source public; the MIT license already allows anyone to use it.
3. Click **Publish repository**. The upload takes a minute or two.
4. Choose **Repository > View on GitHub**. The README renders on the front page with the screenshots.

### A6. Finish the page

On the GitHub page, next to **About**, click the gear icon:

- **Description**: the one-line pitch from `docs/PITCH.md`.
- **Website**: the Unity Play link once Part B is done.
- **Topics**: `unity`, `game`, `puzzle`, `first-person`, `gravity`, `csharp`.

Under **Settings > General**, consider turning off Wikis and Projects to keep the page tidy, and leave Issues on so players can report problems.

### A7. Later changes

Edit the project, come back to GitHub Desktop, write a summary, **Commit to main**, then **Push origin**. That is the whole loop.

### A8. The same thing from the command line

If you prefer a terminal, install Git from https://git-scm.com/download/win, then in the project folder:

```
git init -b main
git add .
git commit -m "Initial import of Gravitiria"
```

Create an empty repository on GitHub (the **+** menu, **New repository**, no README, no license), copy its address, then:

```
git remote add origin https://github.com/<username>/Gravitiria.git
git push -u origin main
```

Git asks you to sign in through the browser the first time.

---

## Part B: A browser version on Unity Play

Unity Play (https://play.unity.com) hosts WebGL builds for free and gives you a link anyone can open. Publishing happens from inside the Unity editor.

### B1. Create a Unity ID

1. Open https://id.unity.com and choose **Create a Unity ID**.
2. Sign up with an email address and password (or Google, Apple, Facebook). Confirm the email.
3. This ID is used by Unity Hub, the editor and Unity Play. The Personal plan is free for projects of this size.

### B2. Install Unity Hub, the editor and the Web module

1. Download Unity Hub from https://unity.com/download and install it. Sign in with the Unity ID.
2. In the Hub, go to **Installs > Install Editor**. Pick the version written in `ProjectSettings/ProjectVersion.txt` (6000.0.3f1). If it is not listed, open the **Archive** tab, or choose the newest 6000.x LTS; the project opens in either.
3. On the **Add modules** page, tick **Web Build Support**. Tick **Android Build Support** too if you plan to do Part C's APK. Click **Install**.
4. If the Hub reports *Install failed: Validation Failed* for the editor, retry without the optional modules first, run the Hub as administrator, make sure the drive has at least 15 GB free and the install path has no Japanese characters, then add the modules afterwards.

### B3. Open the project

1. **Projects > Add > Add project from disk**, choose the project folder, and open it.
2. The first open takes a few minutes. If the editor offers to upgrade the project version, accept.
3. If the Console shows errors, the usual cause is a missing module. Nothing in the project needs a package you have to install by hand.

### B4. Switch to Web and check the settings

1. **File > Build Profiles** (Ctrl+Shift+B).
2. Under **Platforms** select **Web**. If it is greyed out with "No Web module loaded", go back to B2.
3. Click **Switch Platform** and wait for the re-import.
4. The **Scene List** should show, in order: Title, Tutorial, Level1, Level2, Level3, Level4, Level5, R1, R2, R3, R4, R5, Ending. Title must be first.
5. Click **Player Settings...** at the bottom of the window and check:
   - **Resolution and Presentation**: Default Canvas Width 1280, Height 720. The game scales its UI to any size.
   - **Publishing Settings > Compression Format**: **Gzip**, and tick **Decompression Fallback**. This combination works on Unity Play and on any plain web host.
   - **Other Settings > Color Space**: leave as is.
6. Close Player Settings.

### B5. Build

1. In Build Profiles click **Build**.
2. Create a new empty folder, for example `WebBuild\Gravitiria_1_0`, and select it.
3. The first Web build takes 10 to 20 minutes. Later builds are faster.
4. To try it locally, click **Build And Run** instead; the editor starts a small server and opens the browser. Opening `index.html` directly from the disk does not work in most browsers.

### B6. Publish to Unity Play

1. With Build Profiles still open, click **Publish to Play** at the bottom of the window.
   If the button is missing, open **Window > Package Manager**, choose **Unity Registry**, search for **WebGL Publisher**, click **Install**, then use the new top menu **Publish > WebGL Project**.
2. The **Publish to Unity Play** window shows the latest build. Click **Publish**. The editor zips the build and uploads it (limit 500 MB; this game is far below).
3. A browser tab opens on your new Unity Play page.

### B7. Fill in the page

On the Unity Play page:

- **Title**: Gravitiria
- **Description**: the one-paragraph or three-paragraph text from `docs/PITCH.md`.
- **Visibility**: Public, or Unlisted if you only want to share the link.
- **Media**: upload `docs/Title.png` as the thumbnail; add two or three screenshots.
- Click **Update**.

Copy the page address. Put it in the GitHub repository's **Website** field and in the README if you like.

### B8. What to check in the browser

- The title shows and music starts after the first click (browsers block audio until the user interacts).
- Clicking **Start** locks the mouse pointer for looking around. Esc releases it; clicking the canvas locks it again.
- Right-click aiming works; the browser context menu does not appear.
- Keyboard focus stays in the game. If keys scroll the page instead, click the canvas once.
- The run timer, death counter and records persist after a reload (Unity Play keeps them in the browser's storage).

### B9. Updating later

Build again, click **Publish to Play**, choose **Update existing game** in the publish window and pick Gravitiria. The link stays the same.

---

## Part C: Downloadable builds on GitHub

Players who do not want a browser version can download a Windows zip or an Android APK from the repository's **Releases** page. Build them first:

- Windows: run `BuildScript.BuildWindows` from the command line as described in the README, then zip the output folder (the `.exe`, the `_Data` folder, `MonoBleedingEdge`, `UnityPlayer.dll`, `UnityCrashHandler64.exe`, `D3D12`).
- Android: run `AndroidBuildSettings.BuildDevelopmentApk`, or in the editor **Build > Android > Build Development APK**. Change `PackageName` in `Assets/Editor/AndroidBuildSettings.cs` to your own identifier first.

Then on GitHub:

1. On the repository page click **Releases** (right column) then **Create a new release** (or **Draft a new release**).
2. **Choose a tag**: type `v1.0.0` and click **Create new tag: v1.0.0 on publish**.
3. **Release title**: `Gravitiria 1.0`.
4. **Describe this release**: paste the 1.0 section of `CHANGELOG.md`.
5. Drag the zip and the APK into the **Attach binaries** box. Each file may be up to 2 GB.
6. Click **Publish release**. The README's "Releases page" link now works.

For the APK, tell players they must allow installation from unknown sources on their phone, since it is not on Google Play.
