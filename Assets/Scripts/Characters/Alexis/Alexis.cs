﻿using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class Alexis : ZodiacCharacter {
	public GameManager manager;

	public List<AudioClip> clips;
	public List<AudioClip> dmgTknClips;
	private AudioSource aSource;
	private Animator anim;

    //controller prefix in parent
    //public string controller;

    // Movement
    private int speed; // Speed tier 1-6
    //public int coins = 0; // Number of coins 0-15

    // Jump
    [SerializeField]
    public int jumpHeight;
    private bool isGrounded;

    // Status
    //private bool isStunned;
    private bool isAlive;

    // Basic Attack
    [SerializeField]
    [Tooltip("How many coins lost when attack hits.")]
    private int bDamage;
    [SerializeField]
    [Tooltip("How long before the attack can be used again.")]
    private int bCoolDown;
    //main AOE
	public GameObject BlastArea;

    // Special Attack
    [SerializeField]
    [Tooltip("How many coins lost when attack hits.")]
    private int spDamage;
	public float delaySpecial;
	public float spCooldown;
	public GameObject granade;
	public Transform launchPoint;

    // Sustained Attack
    [SerializeField]
    [Tooltip("How many coins lost when attack hits.")]
    public int hDamage;
	public bool isSusAttacking = false;

    [HideInInspector]
    public bool haveItem = false;
    private GameObject inventory;

	public float delayBasic;
	GameObject temp;

    private bool canAttackBasic = true;
    private bool canAttackSpecial = true;
	public float stunDur;
	public float InvincTime;
	public UIManager uiMan;

	public float horForce;
	public float vertForce;

    // Use this for initialization
    void Start () {
		transform.GetChild (1).gameObject.SetActive (false);
		aSource = GetComponent<AudioSource>();
		anim = GetComponent<Animator>();
		uiMan = myHUD.GetComponent<UIManager>();
		isStunned = false;
    }
	// Update is called once per frame
	void FixedUpdate () {
		if(!isStunned){
			//basic attack
			if ((Input.GetAxis(controller + "BA") > 0.5f || Input.GetKeyDown(KeyCode.J))&& canAttackBasic) {
        	    BlastArea.GetComponent<ShotGunMain>().BasicAttack();
            	//call anim
				anim.SetTrigger("BasicAttack");
				aSource.PlayOneShot(clips[0]);
				//manager.StatUpdate (controller, "Attacks", bDamage);
            	StartCoroutine(AttackBasicDelay());
	        }
			// special attack
			if ((Input.GetAxis(controller + "SpA") > 0.5f  || Input.GetKeyDown(KeyCode.K)) && canAttackSpecial) {
                canAttackSpecial = false;
                StartCoroutine(AttackSpecialDelay());
			}
			if ((Input.GetAxis(controller + "ItemUse") > 0.5f || Input.GetAxis("Fire1") > 0.5f) && haveItem){
        	    // Use the pickup
            	Debug.Log("Used " + inventory);
				switch(inventory.GetComponent<SpriteRenderer>().sprite.name){
				case "powerup":
					StartCoroutine (Invincible());
					break;
				default:
					break;
				}
				uiMan.ItemDisplay("Default");
            	haveItem = false;
        	}
			if ((Input.GetAxis(controller + "ItemDrop") > 0.5f || Input.GetAxis("Fire2") > 0.5f) && haveItem){
            	// Drop the pickup
            	inventory.transform.position = new Vector3(this.gameObject.transform.position.x - 2.5f, this.gameObject.transform.position.y, inventory.transform.position.z);
            	inventory.SetActive(true);
				// apply a force to make it move
				//GameObject child = inventory.transform.GetChild(0).gameObject;
				Rigidbody2D rb = inventory.GetComponent<Rigidbody2D>();
				//child.SetActive(true);
				rb.isKinematic = false;
				rb.AddForce(new Vector2(horForce, vertForce), ForceMode2D.Impulse);

				Debug.Log("Dropped " + inventory);
				uiMan.ItemDisplay("Default");
				haveItem = false;
        	}

			if(Input.GetAxis(controller + "SuA") > 0f){
				anim.SetBool ("SustainedAttack", true);
				isSusAttacking = true;
				aSource.PlayOneShot (clips [2]);
				Debug.Log ("SusAttacking start");
			}
			else{
				isSusAttacking = false;
				anim.SetBool ("SustainedAttack", false);
				Debug.Log ("SusAttacking end");
			}
		}
    }
    public IEnumerator AttackBasicDelay(){
        canAttackBasic = false;
		aSource.PlayOneShot(clips[0]);
		yield return new WaitForSeconds(delayBasic);
        canAttackBasic = true;
    }
    public IEnumerator AttackSpecialDelay()
    {
        anim.SetTrigger("SpecialAttack");
        yield return new WaitForSeconds(delaySpecial);
        temp = Instantiate(granade, launchPoint.position, Quaternion.identity) as GameObject;
        temp.GetComponent<Grenade>().owner = this.gameObject;
        if (!GetComponent<CharacterMovement>().facingRight)
            temp.GetComponent<Grenade>().HSpeed *= -1;
		aSource.PlayOneShot(clips [1]);
        yield return new WaitForSeconds(spCooldown);
        canAttackSpecial = true;
    }
    public void OnTriggerEnter2D(Collider2D other){
        if (other.gameObject.CompareTag("Coin")){
            coins++;
			aSource.PlayOneShot (clips [3]);
			manager.StatUpdate (controller, "MC", 1);
            //Debug.Log("Total Coins = " + coins);
            Destroy(other.gameObject);
        }
        if (other.gameObject.CompareTag("Item") && !haveItem){
            inventory = other.gameObject;
            Debug.Log("Picked up " + inventory);
			aSource.PlayOneShot (clips [4]);
			uiMan.ItemDisplay(other.GetComponent<SpriteRenderer>().sprite.name);
            inventory.SetActive(false);
            haveItem = true;
        }
    }
	public override void TakeDamage(int _damage){
		int i = Random.Range (0, 4);

		aSource.PlayOneShot(dmgTknClips[i]);
		anim.SetTrigger ("TakeDamage");
		if (coins - _damage <= 0)
			coins = 0;
		else
			coins -= _damage;
        Debug.Log("Coins" + coins);
		manager.StatUpdate (controller, "MDT", _damage);
		Debug.Log ("Damage Taken Track " + i);
		StartCoroutine (Stun());
    }
	public void AttackUpdate(int amount){
		manager.StatUpdate (controller, "MDG", amount);
	}
	public IEnumerator Stun(){
		isStunned = true;
		yield return new WaitForSeconds(stunDur);
		isStunned = false;
	}
	public IEnumerator Invincible(){
		isInvincible = true;
		transform.GetChild (1).gameObject.SetActive (true);
		yield return new WaitForSeconds(InvincTime);
		transform.GetChild (1).gameObject.SetActive (false);
		isInvincible= false;
	}
}
