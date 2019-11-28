using System;
using System.IO;
using LSDR.InputManagement;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Torii.Binding;
using Torii.Serialization;
using Torii.Util;
using UnityEngine;
using UnityEngine.Audio;

namespace LSDR.Game
{
    [CreateAssetMenu(menuName="System/SettingsSystem")]
    public class SettingsSystem : ScriptableObject
    {
        // reference to master audio mixer used for volume controls
        public AudioMixer MasterMixer;
        
        public GameSettings Settings { get; private set; }
        
        /// <summary>
        /// Used to disable player motion, i.e. when linking.
        /// </summary>
        public bool CanControlPlayer = true;

        /// <summary>
        /// Used to disable mouse looking, i.e. when paused. Please use SetCursorViewState().
        /// </summary>
        public bool CanMouseLook = true;

        public bool SubtractiveFog = false;
        
        public BindBroker SettingsBindBroker = new BindBroker();

        /// <summary>
        /// Whether or not the game is currently paused.
        /// TODO: move to separate scriptableobject and event system
        /// </summary>
        public bool IsPaused = false;

        /// <summary>
        /// Whether or not we're in VR mode.
        /// </summary>
        public bool VR;
        
        /// <summary>
        /// The framerate of the PS1. Used when framerate limiting is enabled.
        /// </summary>
        public const int FRAMERATE_LIMIT = 25;

        public Shader ClassicDiffuse;
        public Shader ClassicAlpha;
        public Shader RevampedDiffuse;
        public Shader RevampedAlpha;

        public Material LBDDiffuse;
        public Material LBDAlpha;

        // reference to serializer used for loading/saving data
        private readonly ToriiSerializer _serializer = new ToriiSerializer();

        // the path to the settings serialized file
        private static string SettingsPath => PathUtil.Combine(Application.persistentDataPath, "settings.json");

        public void OnEnable()
        {
            VR = !UnityEngine.XR.XRSettings.loadedDeviceName.Equals(string.Empty);
            
            _serializer.RegisterJsonSerializationSettings(typeof(GameSettings), new JsonSerializerSettings()
            {
                Formatting = Formatting.Indented,
                SerializationBinder = new DefaultSerializationBinder()
            });
        }

        public void Load()
        {
            Debug.Log("Loading game settings...");

            // if we're loading over an existing settings object we want to deregister the old one
            if (Settings != null)
            {
                SettingsBindBroker.DeregisterData(Settings);
            }
		    
            // check to see if the settings file exists
            if (File.Exists(SettingsPath))
            {
                Settings = _serializer.Deserialize<GameSettings>(SettingsPath);
            }
            else
            {
                // create the default settings
                Debug.Log("Settings.json not found, creating default settings");
                Settings = new GameSettings();
                Save();
            }
            
            // register the new settings object
            SettingsBindBroker.RegisterData(Settings);
        }

        public void Save()
        {
            Debug.Log("Saving game settings...");

            _serializer.Serialize(Settings, SettingsPath);
        }
        
        /// <summary>
        /// Apply the game settings. This function propagates the given settings to all game systems that need them.
        /// </summary>
        public void Apply()
        {
            Debug.Log("Applying");
            
            // TODO: try and catch exceptions for erroneous loaded values (i.e. array idx) and reset to default if error
		    
            // set the control scheme
            ControlSchemeManager.UseScheme(Settings.CurrentControlSchemeIndex);

            // set the resolution
            if (Settings.CurrentResolutionIndex > Screen.resolutions.Length)
            {
                // if the resolution is invalid, set it to the lowest resolution
                Screen.SetResolution(Screen.resolutions[0].width, Screen.resolutions[0].height, Settings.Fullscreen);
            }
            else
            {
                Screen.SetResolution(Screen.resolutions[Settings.CurrentResolutionIndex].width,
                    Screen.resolutions[Settings.CurrentResolutionIndex].height, Settings.Fullscreen);
            }
			
            // set framerate to limit or not
            Application.targetFrameRate = Settings.LimitFramerate ? FRAMERATE_LIMIT : -1;
			
            // set retro shader affine intensity
            Shader.SetGlobalFloat("AffineIntensity", Settings.AffineIntensity);
			
            // set the current dream journal
            DreamJournalManager.SetJournal(Settings.CurrentJournalIndex);
			
            // set volumes
            SetMusicVolume(Settings.MusicVolume);
            SetSFXVolume(Settings.SFXVolume);
            
            // set the graphics quality
            QualitySettings.SetQualityLevel(Settings.CurrentQualityIndex, true);

            // update any shaders
            if (Settings.UseClassicShaders)
            {
                LBDDiffuse.shader = ClassicDiffuse;
                LBDAlpha.shader = ClassicAlpha;
            }
            else
            {
                LBDDiffuse.shader = RevampedDiffuse;
                LBDAlpha.shader = RevampedAlpha;
            }
        }
        
        /// <summary>
        /// Set the cursor view state. True sets the cursor to visible and unlocks it, false does the inverse.
        /// </summary>
        /// <param name="state">Cursor state to set.</param>
        public void SetCursorViewState(bool state)
        {
            Cursor.visible = state;
            Cursor.lockState = state ? CursorLockMode.None : CursorLockMode.Locked;
        }

        /// <summary>
        /// Change the game's pause state. Pausing the game will enable the mouse pointer.
        /// </summary>
        /// <param name="pauseState">The pause state to set.</param>
        public void PauseGame(bool pauseState)
        {
            IsPaused = pauseState;
            SetCursorViewState(pauseState);
            Time.timeScale = pauseState ? 0 : 1;
        }
        
        /// <summary>
        /// Set the music volume.
        /// </summary>
        /// <param name="val">Music volume in percentage.</param>
        public void SetMusicVolume(float val)
        {
            Debug.Log(volumeToDb(val));
            MasterMixer.SetFloat("MusicVolume", volumeToDb(val));
        }

        /// <summary>
        /// Set the SFX volume.
        /// </summary>
        /// <param name="val">SFX volume in percentage.</param>
        public void SetSFXVolume(float val)
        {
            MasterMixer.SetFloat("SFXVolume", volumeToDb(val));
        }
        
        // convert a volume percentage into decibels
        private static float volumeToDb(float volume)
        {
            if (volume <= 0) return -80;
            return 20 * Mathf.Log10(volume);
        }
    }
}