Recent updates. as of Sept 17, 2022
Textures, sculpts, and meshes are processed by an external thread before being fed into the main thread. This has greatly improved performance.
HOWEVER, the client is eating an inordinate amount of ram to do its thing, and I highly suspect it's the way I'm handling LOD and will be fixing it.

# CrystalFrost
Open source Unity 2021.3.6f1 LTS based Second Life viewer using LibreMetaverse.

At current there is no working build, but the source code does work. It let syou log in and spawns boxes at the right scale, position, and rotation for most of the objects in the current sim. There is some basic movement and teleport code but it's not tied into any controls at the moment.

Unless otherwise noted at the top of a given file, the terms of use and redistribution for the source code for this project are that you may download and modify the source for your own personal (but not commercial) use, but may not distribute any changes without written permission from copyright holder of CrystalFrost, known on Github as JennaScvl. We do however welcome submissions and improvements, as well as people who wish to join the team.

This project is neither endorsed by nor associated with but uses code and content that is licensed from openmetaverse.co and Sjofn LLC.
Below is the required copyright notice, which unless otherwise noted applies SOLELY to the code and content in the libremetaverse folder and to no other code in the project.

 * Copyright (c) 2006-2016, openmetaverse.co
 * Copyright (c) 2019-2022, Sjofn, LLC
 * All rights reserved.
 *
 * - Redistribution and use in source and binary forms, with or without 
 *   modification, are permitted provided that the following conditions are met:
 *
 * - Redistributions of source code must retain the above copyright notice, this
 *   list of conditions and the following disclaimer.
 * - Neither the name of the openmetaverse.co nor the names 
 *   of its contributors may be used to endorse or promote products derived from
 *   this software without specific prior written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" 
 * AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE 
 * IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE 
 * ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE 
 * LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR 
 * CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF 
 * SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS 
 * INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN 
 * CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) 
 * ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE 
 * POSSIBILITY OF SUCH DAMAGE.
