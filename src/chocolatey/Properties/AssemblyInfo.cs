// Copyright © 2017 - 2025 Chocolatey Software, Inc
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

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// The following GUID is for the ID of the typelib if this project is exposed to COM

[assembly: Guid("bd59231e-97d1-4fc0-a975-80c3fed498b7")]

[assembly: InternalsVisibleTo("chocolatey.tests, PublicKey=002400000480000094000000060200000024000052534131000400000100010059b0331d79eca3ed9087221c6252d3383087f35fa7c54f2ce6223c40905ed2c44e2de8576ae81bcd2f0471961bdbc083a8457d564912d3d3263477756337565ac8ce8dfe912b15cd762ac9e5c4641d82768d9da09265cde5b414ed08e654a3ebf48b9c70b34ebeab0adcb145c439289b5fa8790ef218a6d8123c8d02251c33cf")]

[assembly: InternalsVisibleTo("chocolatey.tests.integration, PublicKey=002400000480000094000000060200000024000052534131000400000100010059b0331d79eca3ed9087221c6252d3383087f35fa7c54f2ce6223c40905ed2c44e2de8576ae81bcd2f0471961bdbc083a8457d564912d3d3263477756337565ac8ce8dfe912b15cd762ac9e5c4641d82768d9da09265cde5b414ed08e654a3ebf48b9c70b34ebeab0adcb145c439289b5fa8790ef218a6d8123c8d02251c33cf")]

// We allow the officially built chocolatey.extension to always see the internals.
[assembly: InternalsVisibleTo("chocolatey.licensed, PublicKey=002400000480000094000000060200000024000052534131000400000100010001f55d4a9065e32d5e9854e592ffa5f7b3a707f55a17796937faf70f3ade21346dcf735216015d20304acd25d260d01202a390ac648ace0e93f6c4d6ac7cbede5b3e8f66e536d03ffa2d09594ac8de7bd147419c17e0fa1fa112b81b1b65a9e8b0ca148dc3a77e7b2917f448455ce9dbad266351710d097424692be8854704e8")]
[assembly: InternalsVisibleTo("chocolatey.interfaces, PublicKey=002400000480000094000000060200000024000052534131000400000100010001f55d4a9065e32d5e9854e592ffa5f7b3a707f55a17796937faf70f3ade21346dcf735216015d20304acd25d260d01202a390ac648ace0e93f6c4d6ac7cbede5b3e8f66e536d03ffa2d09594ac8de7bd147419c17e0fa1fa112b81b1b65a9e8b0ca148dc3a77e7b2917f448455ce9dbad266351710d097424692be8854704e8")]

#if !FORCE_CHOCOLATEY_OFFICIAL_KEY
[assembly: InternalsVisibleTo("chocolatey.licensed, PublicKey=00240000048000009400000006020000002400005253413100040000010001003f70732af6adf3f525d983852cc7049878c498e4f8a413bd7685c9edc503ed6c6e4087354c7c1797b7c9f6d9bd3c25cdd5f97b0e810b7dd1aaba2e489f60d17d1f03c0f4db27c63146ee64ce797e4c92d591a750d8c342f5b67775710f6f9b3d9d10b4121522779a1ff72776bcce3962ca66f1755919972fb70ffb289bc082b3")]
[assembly: InternalsVisibleTo("chocolatey.interfaces, PublicKey=00240000048000009400000006020000002400005253413100040000010001003f70732af6adf3f525d983852cc7049878c498e4f8a413bd7685c9edc503ed6c6e4087354c7c1797b7c9f6d9bd3c25cdd5f97b0e810b7dd1aaba2e489f60d17d1f03c0f4db27c63146ee64ce797e4c92d591a750d8c342f5b67775710f6f9b3d9d10b4121522779a1ff72776bcce3962ca66f1755919972fb70ffb289bc082b3")]
#endif