WpiArcadeMachine
================

This is the repo for the WPI Arcade Machine. If you would like to work on the machine please contact gda-execs@wpi.edu.

Before you will be allowed to code you <b>must</b> read the coding standards specified below!

Coding Standards
================

The following pertains to how code on the repo will be handled. If you are a lead developer you <b>must</b> follow these guide lines.

<h2>Issues</h2>

Sense this is a large code project with multiple people working on it, it's very important that people know what they are working on. Luckily, github provides means to do this. You will notice on github there is an issues section populated with issues. These are the current tasks that must be completed for the machine. As a normal user on a fork, you are free to work on any <b>low</b> level issue. We will reject any code that is for higher level issues if it is not for a lead developer. This is to ensure that code is not duplicated. Low level tasks may still be rejected if the low level task is already assigned so make sure to double check that before working on a low level task.

As a lead developer however you have a bit more freedom. You are free to work on any level issue. It is important to make sure that before working on an issue, make sure to assign it to yourself. Likewise you may assign, or be assigned issues by other members. If assigned an issue make sure to either address it, or re-assign it to someone else if you are un-able to. This is simply to ensure that issues do not go stale.

As a lead developer you can also create new issues. When creating an issue you should have a descriptive title, a full description of the issue, and at least three labels following this template [priority][section][type]
where

[priority] is either low, medium, or high. Where low is this is an issue that does not ever need to be addressed, medium is an issue that should be addressed however if not it would not be fatal, and high is an issue that must be addressed (preferebly in a timely manner).

[section] is what section of the repository this issue is affecting. At least one section should be listed.

[type] is what type of issue this is. This can range from bug to feature.

You should also label what branch the issue is on if it is not on master. Otherwise it will be assumed to be on master.

<h2>Commits as a Lead</h2>

<b>When committing you should always commit with rebase, never with merge.</b>

When addressing an issue, your commits should follow the following format.

Issue #<issue number> <short description>

<long description>

Tested By: <Test method>

An example would be:

<b>
Issue #22 Fixed bug in log file

This commit fixes the bug in the logging code.

Tested By: Logging a test string to the console.
</b>

If a you believe a commit resolves an issue then at the end of the commit you should include a line like so:
Reviewer @<reviewer user name>
Where the reviewer user name is the user name of the person you want to give the code review (more on this later). It is important to always give a reviewer, otherwise your issue will never be resolved.

If your commit is for a code review it should follow this format:

CR for #<issue number> <short description>

<long description>

Tested By: <Test method>

Reviewer @<reviewer user name>

This is similar to a plain issue commit, however it starts with CR for code review, and it requires a reviewer to be given.

<h2>Code Reviews</h2>

A code review is a process where a second person tests code written by another person to ensure that there code does work. Sense people are working with multiple versions of the repo it ensures that code does actually work.

When a code review is assigned to you, you should look through the code changes made (either in your repo or on githubs diff tool). If you have any questions, comments, or fixes, a comment should be put on the code in the commit on git for questions about specific pieces of code, or in the issue for more general comments. When the reviewer feels that the issue has been resolved the issue should be closed by the reviewer. Until then it is up to the assigned person to answer questions and continue to commit until the reviewer is satisfied.

<h2>Commits as Normal User</h2>

If you are not a Lead Developer, then you will be committing through pull requests rather than pushing directly to the repo. When you create your pull request you should pull with a comment of the following layout:

Issue #<issue number> <short description>

<long description>

Tested By: <Test method>

Your pull request will be picked up by a lead developer who will either accept it into the repo, or reject it with comments.

<h2>Branches</h2>

There are three different types of branches.

Master:
The master branch is the main branch where all working code will be put.

Feature-<feature name>:
This is a branch that contains some experimental features, if you create a feature branch, also make a label for issues for it. A feature branch will always be created from master and merge back into master.

Build-<build number>:
This is a branch that contains some working build of the arcade machine. If you create a build branch, a label for issues should also be created. A build branch will always come from master and will never merge back. Changes made to a build branch are considered specific to that branch.

