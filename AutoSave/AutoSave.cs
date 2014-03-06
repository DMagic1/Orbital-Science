/* DMagic Orbital Science - AutoSave
 * Creates a backup save file upon initial game load
 *
 * Copyright (c) 2014, DMagic
 * All rights reserved.
 * 
 * Redistribution and use in source and binary forms, with or without modification, 
 * are permitted provided that the following conditions are met:
 * 
 * 1. Redistributions of source code must retain the above copyright notice, 
 * this list of conditions and the following disclaimer.
 * 
 * 2. Redistributions in binary form must reproduce the above copyright notice, 
 * this list of conditions and the following disclaimer in the documentation and/or other materials 
 * provided with the distribution.
 * 
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, 
 * INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE 
 * DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, 
 * SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE 
 * GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF 
 * LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT 
 * OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 *  
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace AutoSave
{
    [KSPAddon(KSPAddon.Startup.MainMenu, false)]
    public class AutoSave : MonoBehaviour
    {
        public static bool makeBackup = true;
        public void Start()
        {
            GameEvents.onGameSceneLoadRequested.Add(saveBackup);
        }

        public void saveBackup(GameScenes scene)
        {
            if (makeBackup)
            {
                string folder = HighLogic.fetch.GameSaveFolder;
                scene = HighLogic.LoadedScene;
                if (scene == GameScenes.MAINMENU)
                {
                    var save = GamePersistence.SaveGame("Persistent Backup", folder, 0);
                    makeBackup = false;
                    print("New savefile created in " + folder);
                    GameEvents.onGameSceneLoadRequested.Remove(saveBackup);
                }
            }
        }
        
    }
}
