Contributors
============

The process for contributions is roughly as follows:

 * Through GitHub, or through the [mailing list](https://groups.google.com/forum/#!forum/chocolatey) (preferred), you talk about a feature you would like to see (or a bug), and why it should be in choco.
 * Once you get a nod from one of the Chocolatey team folks (https://github.com/chocolatey?tab=members), you can start on the feature.
 * You create, or update, a fork of Chocolatey under your GitHub account.
 * From there you create a branch named specific to the feature.
 * In the branch you do work specific to the feature. Please also observe the following:
    * No reformatting
    * No changing files that are not specific to the feature
 * Test your changes and please help us out by updating and implementing some automated tests. If you are not familiar with [Pester](https://github.com/pester/Pester), I would suggest just spend some time looking over the tests in the source code. You can't go wrong emulating one of the existing tests and then changing it specific to the behavior you are testing.  You can install Pester with Chocolatey by ```cinst pester```.
 * Once you feel it is ready, submit the pull request to the chocolatey/chocolatey repository against the ````master```` branch ([more information on this can be found here](https://help.github.com/articles/creating-a-pull-request)).
 * In the pull request, outline what you did and point to specific conversations (as in URL's) and issues that you are are resolving. This is a tremendous help for us in evaluation and acceptance.
 * Once the pull request is in, please do not delete the branch or close the pull request (unless something is wrong with it).
 * One of the members will evaluate it within a reasonable time period (which is to say usually within 2-4 weeks). Some things get evaluated faster or fast tracked. We are human and we have active lives outside of open source so don't fret if you haven't seen any activity on your pull request within a month or two. We don't have a Service Level Agreement (SLA) for pull requests. Just know that we will evaluate your pull request.
 * If we have comments or questions when we do evaluate it and receive no response, it will probably lessen the chance of getting accepted.

The helpers/utility functions that are available to the packages are what we consider the API. If you are working in the API, please note that you will need to maintain backwards compatibility. If you plan to rename a function or make it more generic, you must provide an alias in the chocolateyInstaller.psm1 (https://github.com/chocolatey/chocolatey/blob/master/src/helpers/chocolateyInstaller.psm1) as part of what gets exported. You should not remove or reorder parameters, only add optional parameters to the end. They should be named and not positional (we are moving away from positional parameters as much as possible).

If you reformat code or hit core functionality without an approval from a person on the choco team, it's likely that no matter how awesome it looks afterwards, it will probably not get accepted. Reformatting code makes it harder for us to evaluate exactly what was changed.

If you do these things, it will be make evaluation and acceptance easy. Now if you stray outside of the guidelines we have above, it doesn't mean we are going to ignore your pull request. It will just make things harder for us.  Harder for us roughly translates to a longer SLA for your pull request.

