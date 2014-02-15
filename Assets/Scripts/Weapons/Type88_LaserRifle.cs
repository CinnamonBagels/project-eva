using UnityEngine;
using System.Collections;

public class Type88_LaserRifle : Weapon_Stats {

	public GameObject debris;

	void Awake() {
		swapRate = 1.0f;
		reloadRate = 0.0f;
		coolDown = 0.0f;
		maxAmmo = 30.0f;
		maxSpare = 0.0f;
		range = 1000000.0f;
		damage = 20.0f;
		bulletSpread = 0.0f;
		bulletCircleRadius = 0.0f;

		player = GameObject.Find( "PlayerAndCamera" );

		WeaponAwake();
	}

	void Start () {
		WeaponStart();
	}
	
	// Update is called once per frame
	void Update () {
		IsSwappingCheck();

		if ( Input.GetButtonDown( InputConstants.AltFire ) ) {
			altFire = AltFireToggle();
		}

		if ( !altFire && hasAmmo && currentSwapRate <= 0 ) {
			Fire();
		}

		if ( altFire && hasAmmo && currentSwapRate <= 0 ) {
			AltFire();
		}

		Debug.Log( "Ammo: " + currentAmmo + " Spare: " + currentSpareAmmo );

	}

	private void AltFire() {
		//TODO
	}

	private void Fire() {
		currentCoolDown -= Time.deltaTime;

		if ( Input.GetButton( InputConstants.Fire ) && currentCoolDown <= 0 ) {
			currentCoolDown = coolDown;

			//sound
			//flare
			//lasertracer

			Vector3 bulletSpreadCircle = Random.insideUnitCircle * bulletCircleRadius;

			Vector3 rayDirection = new Vector3(
				player.transform.forward.x + ( bulletSpread * bulletSpreadCircle.x ),
				player.transform.forward.y + ( bulletSpread * bulletSpreadCircle.y ),
				Camera.main.transform.forward.z );

			Ray ray = new Ray( Camera.main.transform.position, rayDirection );

			if ( Physics.Raycast( ray, out hitInfo, range ) ) {
				Vector3 hitPoint = hitInfo.point;

				GameObject hitObject = hitInfo.collider.gameObject;

				//if hit enemy tag

				//if hit cover tag

				

				Debug.DrawLine( player.transform.position, hitPoint );
			}
			currentAmmo -= Time.deltaTime * 2;

			if ( currentAmmo <= 0 ) {
				hasAmmo = false;
			}
		}
	}
}
