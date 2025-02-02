using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace IrfanQavi.PlayerController {

    public class CameraFollow : MonoBehaviour {

        [SerializeField] private Transform _headOfPlayer;

        private void Update() {

            transform.position = _headOfPlayer.position;

        }

    }

}
