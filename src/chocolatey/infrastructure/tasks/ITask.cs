// Copyright © 2017 - 2018 Chocolatey Software, Inc
// Copyright © 2011 - 2017 RealDimensions Software, LLC
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// 
// You may obtain a copy of the License at
// 
// 	http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

namespace chocolatey.infrastructure.tasks
{
    /// <summary>
    ///   Interface for all runners.
    /// </summary>
    public interface ITask
    {
        /// <summary>
        ///   Initializes a task. This should be initialized to run on a schedule, a trigger, a subscription to event messages,
        ///   etc, or some combination of the above.
        /// </summary>
        void initialize();

        /// <summary>
        ///   Shuts down a task that is in a waiting state. Turns off all schedules, triggers or subscriptions.
        /// </summary>
        void shutdown();
    }
}
