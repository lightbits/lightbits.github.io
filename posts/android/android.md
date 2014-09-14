![Title shot](/posts/android/IMG_2834.JPG)

I've recently purchased a Galaxy Nexus, and while my previous phone had some unknown proprietary OS, this one has Android! Version 4.0.2 even! I love to tinker around, so I naturally felt a need to program something for this thing.

However, being spoiled by the luxuries of Visual Studio, I couldn't stand to work with other IDEs like Eclipse or IntelliJ IDEA. So I decided to work with the SDK command line and figure out how to compile stuff manually. But going through the process by hand every time can be tiring, so I wrote a plugin for my favorite code editor, Sublime Text 2.

I should mention that I had no prior Python experience when I spent a single sunday evening writing this plugin. So it might blow up your computer.

##What it should do:##

* Provide a sort-of simple interface for creating new Android projects
* Compile and build your application (signed with a debug key)
* Provide a fully automated build-install system
* Run ADB logcat to see device output (like System.out.println) with continuous updates.

##Getting it working:##

First you need to download the archive below and extract it to "Data/Packages", located in your Sublime Text 2 installation. The archive comes with a file called "android.sublime-settings", which contains information about where Java, Android SDK and Apache Ant are located. You will need to configure these paths like this:

* ``jdk_bin``: The folder you would usually set as JAVA_HOME.
* ``default_android_project_dir``: Up to you
* ``android_sdk``: Android SDK root folder.
* ``ant_bin``: The /bin folder of wherever you have installed apache-ant.

And that's it! After restarting Sublime Text 2, an "Android" menu-item should now show up under the 'Tools' menu, in addition to two build-systems. The "Android" build will attempt to uninstall the package (if it exists), followed by a debug build and installation onto the connected device. "Android (Build only)" will only compile a debug build and create a signed .apk.

Download: [.zip](http://dl.dropbox.com/u/27844576/Releases/st2android.zip)