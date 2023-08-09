// SCIENCE BIRDS: A clone version of the Angry Birds game used for 
// research purposes
// 
// Copyright (C) 2016 - Lucas N. Ferreira - lucasnfe@gmail.com
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>
//

using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Threading;
using System.Diagnostics;
using UnityEngine.SceneManagement;

public class HUD : ABSingleton<HUD> {

    public float _zoomSpeed;
    public float _dragSpeed;

    public RectTransform _scoreDisplay;

    public float _simulateDragTime = 1f;

    public float SimulatedTapTime{ get; private set;}
	public int SimulateInputEvent { get; set; }

	public bool shootDone {get; set;}

	public Vector3 SimulateInputPos{ get; set; }
	public Vector3 SimulateInputDelta{ get; set; }

	private bool _isZoomingIn; 
	private bool _isZoomingOut;
	public bool usedSpecialPower { get; set; }

	private int _totalScore;
	public float simulatedDragTimer;
    public float tapTimer;

	private Vector3 _inputPos;
	private Vector3 _dragOrigin;
	public ABBird selectedBird {get; private set;}

	private long startTime;

	private Stopwatch stopwatch;
	void Start() {
		stopwatch = new Stopwatch();
		SetScoreDisplay(_totalScore);
        this.SimulatedTapTime = 0.0f;
	}

	// Update is called once per frame
	void Update () {

		float scrollDirection = Input.GetAxis("Mouse ScrollWheel");

		if(scrollDirection != 0f) {

			if (scrollDirection > 0f)
				_isZoomingIn = true;
			
			else if (scrollDirection < 0f)
				_isZoomingOut = true;
		}
		
		if(_isZoomingIn) {
			
			if (scrollDirection != 0f) {

				// Zoom triggered via MouseWheel
				_isZoomingIn = false;
				CameraZoom(-ABConstants.MOUSE_SENSIBILITY);
			} 
			else {

				// Zoom triggered via HUD
				CameraZoom(-1f);
			}
			
			return;
		}
		
		if(_isZoomingOut) {
			
			if (scrollDirection != 0f) {

				// Zoom triggered via MouseWheel
				_isZoomingOut = false;
				CameraZoom (ABConstants.MOUSE_SENSIBILITY);
			} 
			else {

				// Zoom triggered via HUD
				CameraZoom(1f);
			}

			return;
		}

        TakeAction();

    }

	public void ClickDown(Vector3 position) {

		_dragOrigin = position;

		Ray ray = Camera.main.ScreenPointToRay(position);
		RaycastHit2D hit = Physics2D.Raycast(ray.origin, ray.direction);

		if (hit && hit.transform.tag == "Bird") {
			
			selectedBird = hit.transform.gameObject.GetComponent<ABBird> ();
			if (selectedBird && !selectedBird.IsSelected && 
				selectedBird == ABGameWorld.Instance.GetCurrentBird ()) {
				
				selectedBird.SelectBird ();
				usedSpecialPower = false;
				return;
			}
		} 
			
		// Trigger special attack
		if (selectedBird && selectedBird.IsInFrontOfSlingshot () &&
		    selectedBird == ABGameWorld.Instance.GetCurrentBird () && 
			!selectedBird.IsDying && !usedSpecialPower) {
			usedSpecialPower = true;
			selectedBird.SendMessage ("SpecialAttack", SendMessageOptions.DontRequireReceiver);
		}
	}

    public void ClickUp() {

		if (selectedBird) {

			if (!selectedBird.IsFlying && !selectedBird.IsDying && 
				selectedBird == ABGameWorld.Instance.GetCurrentBird ()) {

				selectedBird.LaunchBird ();
			}
		}
	}

	public void Drag(Vector3 position) {

		if(selectedBird) {

			if (!selectedBird.IsFlying && !selectedBird.IsDying && 
				selectedBird == ABGameWorld.Instance.GetCurrentBird ()) {
				
				Vector3 dragPosition = Camera.main.ScreenToWorldPoint(position);
				dragPosition = new Vector3(dragPosition.x, dragPosition.y, selectedBird.transform.position.z);

				selectedBird.DragBird(dragPosition);
			}

		}
		else {
			
			Vector3 dragPosition = position - _dragOrigin;
			ABGameWorld.Instance.GameplayCam.DragCamera(dragPosition * _dragSpeed * Time.fixedDeltaTime);
		}
	}

	private void SetZoomIn(bool zoomIn) {
		
		_isZoomingIn = zoomIn;
	}
	
	private void SetZoomOut(bool zoomOut) {
		
		_isZoomingOut = zoomOut;
	}
	
	public void CameraZoom(float scrollDirection) {

		ABGameWorld.Instance.GameplayCam.ZoomCamera(scrollDirection * _zoomSpeed * Time.deltaTime * ABGameWorld.SimulationSpeed);
	}

	private void SetScoreDisplay(int score) {
		
		if(_scoreDisplay) {
			
			_totalScore = score;
			_scoreDisplay.GetComponent<Text>().text = _totalScore.ToString();
		}
	}

	public void AddScore(int score) {
		
		_totalScore += score;

		// prevent the total score going negative
		if (_totalScore < 0)
			_totalScore = 0;

		_scoreDisplay.GetComponent<Text>().text = _totalScore.ToString();
	}

	public int GetScore() {

		return _totalScore;
	}


    //set the maximal tap time to be 5000ms
    //to prevent the block of game when it is set to too large
    public void SetTapTime(float tapTime) {

        if (tapTime <= 5000) {
            this.SimulatedTapTime = tapTime;
        }
        else {
            this.SimulatedTapTime = 5000;
        }

    }


    public void TakeAction()
    {

//        bool isMouseControlling = true;
        float simulateDragTime = 1;
        Vector3 _inputPos = Input.mousePosition;

        if (Input.GetMouseButtonDown(0))
        {
            //To ensure the human player still can play the game
            //Unpaused when mouse click is detected
            Time.timeScale = 1.0f;
            HUD.Instance.ClickDown(_inputPos);
        }
        else if (Input.GetMouseButton(0))
        {

            for (int i = 0; i < ABGameWorld.SimulationSpeed; i++)
            {
                HUD.Instance.Drag(_inputPos);
            }
            HUD.Instance.simulatedDragTimer += Time.fixedDeltaTime;
            if (HUD.Instance.simulatedDragTimer >= simulateDragTime)
            {
                HUD.Instance.simulatedDragTimer = 0f;
            }
        }
        else if (Input.GetMouseButtonUp(0))
        {
            HUD.Instance.ClickUp();

        }
    }

}
