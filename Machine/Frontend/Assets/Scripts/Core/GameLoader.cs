using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Read the games config file and load in all the assets it needs.
/// </summary>
namespace IniParser{
	public class GameLoader : MonoBehaviour {
		string assetSection = "Assets";
		string teamSection = "team";
		string nameSection = "name";

		// Use this for initialization
		void Start () {

		}
		
		// Update is called once per frame
		void Update () {
			
		}

		//read the games config file and load the assets
		/// <param name="directory"> directory to load game assets from</param>
		public void LoadAssets(string directory){
			IniParser.FileIniDataParser parser = new IniParser.FileIniDataParser();
			IniParser.Model.IniData configData = parser.ReadFile (directory);
			IniParser.Model.KeyDataCollection gameInfo = configData[assetSection];
			string teamName = gameInfo.GetKeyData(teamSection).Value;
			string gameName = gameInfo.GetKeyData(nameSection).Value; 
		}
	}
}