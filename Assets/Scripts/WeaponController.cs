using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.Animations.Rigging;

[System.Serializable] public class AmmoEvent : UnityEngine.Events.UnityEvent<int, int> { }
[System.Serializable] public class MagazineEvent : UnityEngine.Events.UnityEvent<int> { }
public abstract class WeaponController : MonoBehaviour {
    public WeaponSetting weaponSetting;

    [Header("FX")]
    public ParticleSystem muzzleFlash;

    [Space(10f)]

    [Header("Weapon Socket")]
    public Transform bulletSpawnPoint;
    public Transform casingSpawnPoint;

    [Space(10f)]

    [Header("Audio")]
    public AudioClip audioAimIn;
    public AudioClip audioFire;
    public AudioClip audioReload;

    private Camera mainCamera;
    private Animator weaponAnimator;
        public Animator WeaponAnimator {
            get {
                return this.weaponAnimator;
            }
            set {
                this.weaponAnimator = value;
            }
        }
    private AudioSource audioSource;
        public AudioSource AudioSource {
            get {
                return this.audioSource;
            }
            set {
                this.audioSource = value;
            }
        }
    private ImpactMemoryPool impactMemoryPool;
    private CasingMemoryPool casingMemoryPool;
    private float reloadTime;
        public float ReloadTime {
            get {
                return this.reloadTime;
            }
            set {
                this.reloadTime = value;
            }
        }
    private bool isReload;
        public bool IsReload {
            get {
                return this.isReload;
            }
            set {
                this.isReload = value;
            }
        }
    private bool isAimSoundPlay;
    private float lastAttackTime;


    public void Init() {
        this.weaponSetting.currentAmmo = this.weaponSetting.maxAmmo;
        this.weaponSetting.currentMagazine = this.weaponSetting.maxMagazine;

        this.lastAttackTime = 0;
        this.isAimSoundPlay = false;

        this.mainCamera = Camera.main;
        this.audioSource = gameObject.GetComponent<AudioSource>();
        this.weaponAnimator = gameObject.GetComponent<Animator>();
        this.impactMemoryPool = gameObject.GetComponent<ImpactMemoryPool>();
        this.casingMemoryPool = gameObject.GetComponent<CasingMemoryPool>();

        WeaponUIController.instance.onAmmoEvent.Invoke(this.weaponSetting.currentAmmo, this.weaponSetting.maxAmmo);
        WeaponUIController.instance.SetupMagazine(this.weaponSetting.currentMagazine, this.weaponSetting.maxMagazine);
    }

    public void UpdateFire() {
        if (PlayerController.instance.MoveFreeze) {
            return;
        }

        if (this.weaponSetting.isAuto) {
            if (Input.GetMouseButton(0)) {    // ????????? ??????
                if (this.isReload) {    // ????????? ????????? ?????? ??????
                    return;
                }

                if (PlayerController.instance.IsRun) {  // ????????? ???????????? ?????? ??????
                    return;
                }

                if (this.weaponSetting.currentAmmo <= 0) {  // ????????? ???????????? ?????? ??????
                    return;
                }

                StartCoroutine("OnFire");
            }
        }
        else if (this.weaponSetting.isSemi) {
            if (Input.GetMouseButtonDown(0)) {    // ????????? ?????????
                if (this.isReload) {    // ????????? ????????? ?????? ??????
                    return;
                }

                if (PlayerController.instance.IsRun) {  // ????????? ???????????? ?????? ??????
                    return;
                }

                if (this.weaponSetting.currentAmmo <= 0) {  // ????????? ???????????? ?????? ??????
                    return;
                }

                StartCoroutine("OnFire");
            }
        }
    }

    public void UpdateReload() {
        if (Input.GetKeyDown(KeyCode.R) && this.weaponSetting.currentAmmo < this.weaponSetting.maxAmmo && this.weaponSetting.currentMagazine > 0 && !this.isReload) {
            if (PlayerController.instance.IsRun) {  // ????????? ???????????? ????????? ??????
                return;
            }

            StartCoroutine("OnReload");
        }
        else if (this.weaponSetting.currentAmmo <= 0 && !this.isReload && this.weaponSetting.currentMagazine > 0) {
            StartCoroutine("OnReload");
        }
    }    

    public IEnumerator OnFire() {
        if (Time.time - this.lastAttackTime > this.weaponSetting.attackRate) {
            this.lastAttackTime = Time.time;
            this.weaponSetting.currentAmmo -= 1;

            WeaponUIController.instance.onAmmoEvent.Invoke(this.weaponSetting.currentAmmo, this.weaponSetting.maxAmmo);    // ??? ??? UI Invoke
            
            this.muzzleFlash.Play();
            PlayerAnimatorController.instance.animator.Play("Fire", 2, 0);  // Animation(Fire) Play
            AudioController.instance.PlaySoundOneShot(this.audioSource, this.audioFire);
            TwoStepRayCast();
            this.casingMemoryPool.SpawnCasing(this.casingSpawnPoint.position, this.casingSpawnPoint.right);
        }

        yield return null; 
    }

    public virtual IEnumerator OnReload() {
        this.IsReload = true;
        PlayerAnimatorController.instance.IsReload = true;   // Animation(Reload) Play
        this.WeaponAnimator.SetTrigger("Reloading");
        AudioController.instance.PlaySoundOneShot(this.AudioSource, this.audioReload);
            
        yield return new WaitForSeconds(this.ReloadTime);

        this.IsReload = false;
        PlayerAnimatorController.instance.IsReload = false;
        this.weaponSetting.currentAmmo = this.weaponSetting.maxAmmo;
        this.weaponSetting.currentMagazine -= 1;
        WeaponUIController.instance.onAmmoEvent.Invoke(this.weaponSetting.currentAmmo, this.weaponSetting.maxAmmo);    // ??? ??? UI Invoke
        WeaponUIController.instance.onMagzineEvent.Invoke(this.weaponSetting.currentMagazine);     // ?????? ??? UI Invoke
    }

    public void TwoStepRayCast() {
        Ray ray;
        RaycastHit hit;
        Vector3 targetPoint = Vector3.zero;
        
        ray = this.mainCamera.ViewportPointToRay(Vector2.one * 0.5f);   // ?????? ?????? ???????????? Ray
        
        if (Physics.Raycast(ray, out hit, this.weaponSetting.attackDistance)) {     // ?????? ?????????????????? Ray ?????? (?????? ?????????)
            targetPoint = hit.point;
            targetPoint.x = hit.point.x + Random.Range(-this.weaponSetting.spreadRange, this.weaponSetting.spreadRange);
            targetPoint.y = hit.point.y + Random.Range(-this.weaponSetting.spreadRange, this.weaponSetting.spreadRange);
        }
        else {
            targetPoint = ray.origin + ray.direction * this.weaponSetting.attackDistance;   // hit??? null??? ??????, Ray??? ?????? ?????????????????? ??? ??????
        }

        Vector3 attackDirection = (targetPoint - this.bulletSpawnPoint.position).normalized;    // (?????? ????????? - ?????? ??????).?????????

        if (Physics.Raycast(bulletSpawnPoint.position, attackDirection, out hit, this.weaponSetting.attackDistance)) {
            this.impactMemoryPool.SpawnImpact(hit); // Hit Impact

            if (hit.transform.CompareTag("Target")) {
                hit.transform.gameObject.GetComponent<DestructibleObject>().TakeDamage(this.weaponSetting.damage);
            }
        }
    }

    public void UpdateAim() {
        if (Input.GetMouseButton(1) && !PlayerController.instance.IsRun) {  // ????????? ????????? (??????)
            if (!this.isAimSoundPlay) {
                this.isAimSoundPlay = true;
                AudioController.instance.PlaySoundOneShot(this.audioSource, this.audioAimIn);  // Aim In
            }

            PlayerAnimatorController.instance.IsAim = true; // ????????? ?????? ?????????
        }
        else if (Input.GetMouseButtonUp(1)) {
            this.isAimSoundPlay = false;
            PlayerAnimatorController.instance.IsAim = false;
        }

        if (PlayerAnimatorController.instance.IsAim) {
            PlayerAnimatorController.instance.Aiming = 1;   // Blend Tree
        }
        else if (!PlayerAnimatorController.instance.IsAim) {
            PlayerAnimatorController.instance.Aiming = 0;   // Blend Tree
        }
    }
}
