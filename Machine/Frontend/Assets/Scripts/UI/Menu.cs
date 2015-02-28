using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Menu : MonoBehaviour {
	
	private static List<Menu> menulist;
	
	// Use this for initialization
	private void Start () {
		menulist = new List<Menu>();
	}
	
	// Update is called once per frame
	void Update () {
	
	}
	
	//Function for adding a Menu to the global stack!~
	public void Push(){
		if(menulist.Contains(this)){
			menulist.Remove(this);
		}
		menulist.Add(this);
		OnPush();
	}
	
	//Function for removing a Menu from the global stack!~
	public void PopInstance(){
		menulist.Remove(this);
		OnPop();
	}
	
	//Function for removing the Menu at the end of the global stack!!~
	public static void Pop(){
		Menu temp = menulist[menulist.Count-1];
		menulist.Remove(temp);
		OnPop();
	}
	
	//Functions to be overridden by parent classes!~
	protected static void OnPop(){
	}
	
	protected virtual void OnPush(){
	}
	
}