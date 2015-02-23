using UnityEngine;
using System.Collections;
using System.Collections.Generic

public class Menu : MonoBehaviour {
	
	private static List<menu> menulist;
	
	// Use this for initialization
	public void Start () {
		this.menulist = new List<menu>;
	}
	
	// Update is called once per frame
	void Update () {
	
	}
	
	//Function for adding a Menu to the global stack!~
	public void Push(){
		if(menulist.Contains(this)){
			menulist.Remove(this);
		}
		menulist.add(this);
		OnPush();
	}
	
	//Function for removing a Menu from the global stack!~
	public void PopInstance(){
		menulist.Remove(this);
		OnPop();
	}
	
	//Function for removing the Menu at the end of the global stack!!~
	public static void Pop(){
		Menu temp = menulist.last();
		menulist.Remove(temp);
		OnPop();
	}
	
	//Functions to be overridden by parent classes!~
	protected virtual void OnPop(){
	}
	
	protected virtual void OnPush(){
	}
	
}