using UnityEngine;
using System.Collections;
using System.Collections.Generic

public class Menu : MonoBehaviour {
	
	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
	
	}
	
	//Function for adding a Menu to the global stack!~
	void Push(List<Menu> menulist){
		OnPush();
		if(menulist.Contains(this)){
			menulist.Remove(this);
		}
		menulist.add(this);
	}
	
	//Function for removing a Menu from the global stack!~
	void PopInstance(List<Menu> menulist){
		OnPop();
		menulist.Remove(this);
	}
	
	//Function for removing the Menu at the end of the global stack!!~
	static void Pop(List<Menu> menulist){
		OnPop();
		Menu temp = menulist.last();
		menulist.Remove(temp);
	}
	
	//Functions to be overridden by parent classes!~
	virtual void OnPop(){
	}
	
	virtual void OnPush(){
	}
	
}