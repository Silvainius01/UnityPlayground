using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
#if (UNITY_SWITCH && !UNITY_EDITOR) || SWITCH_DEV
using nn.fs;
using nn.account;
using UnityEngine.UI;
#endif

// This code is a for a saving sample that saves/loads a single string.

// Important: This code requires that in the Unity Editor you set PlayerSettings > Publishing Settings > Startup user account to 'Required'.
// This code does not check whether or not there is enough free space on disk to create save date. Instead, it relies on the 'Startup user account'
// setting to verify there is enough free space on device. If you would like to instead manage how your game creates save data space on device,
// see the NintendoSDK plugin and the NintendoSDK documentation.

public class SaveDataHandler : MonoBehaviour
{
    private static SaveDataHandler _instance;
    public static SaveDataHandler instance
    {
        get
        {
            if (_instance == null && !applicationExiting)
            {
                // if no instance of object, create it
                GameObject obj = new GameObject("Save Data Manager");
                _instance = obj.AddComponent<SaveDataHandler>();
            }
            return _instance;
        }
    }
    private static bool applicationExiting = false;

#if (UNITY_SWITCH && !UNITY_EDITOR) || SWITCH_DEV
    private Uid userId; // user ID for the user account on the Nintendo Switch
    private FileHandle fileHandle = new nn.fs.FileHandle();
#endif
    // Save journaling memory is used for each time files are created, deleted, or written.
    // The journaling memory is freed after nn::fs::CommitSaveData is called.
    // For any single time you save data, check the file size against your journaling size.
    // Check against the total save data size only when you want to be sure all files don't exceed the limit.
    // The variable journalSaveDataSize is only a value that is checked against in this code. The actual journal size is set in the
    // Unity editor in PlayerSettings > Publishing Settings > User account save data    
    private const int journalSaveDataSize = (1024 * 512) - (1024 * 2);   // 510 KB. This value should be 32KB less than the journal size
                                                     // entered in PlayerSettings > Publishing Settings
    private string mountName = "";
    private string saveDataPath = "";
    private bool initialized = false;
    private LogicLocker exitRequestHandlerLocker = new LogicLocker();
    private string lockerName = "save data handler";

    public void Awake()
    {
        if (_instance != null && _instance != this)
        {
            // if obj is a duplicate, destroy it
            Destroy(gameObject);
            return;
        }
        else
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }

        lockerName += gameObject.GetInstanceID();

        Initialize();
    }

    public void Initialize()
    {
        if (initialized) return;

        // init save locatioon
#if (UNITY_SWITCH && !UNITY_EDITOR) || SWITCH_DEV
        mountName = "save";
        saveDataPath += mountName + ":/";
#else
        mountName = Application.persistentDataPath;
        saveDataPath += mountName + "/";
#endif

#if (UNITY_SWITCH && !UNITY_EDITOR) || SWITCH_DEV
        // init player prefs which inits mounted save data
        PlayerPrefsSwitch.PlayerPrefsSwitch.Init();
        nn.account.UserHandle userHandle = new nn.account.UserHandle();
        PlayerPrefsSwitch.PlayerPrefsSwitch.GetUserHandle(ref userHandle);
        nn.account.Account.GetUserId(ref userId, userHandle);
#endif
        initialized = true;
    }

    void OnDestroy()
    {
#if (UNITY_SWITCH && !UNITY_EDITOR) || SWITCH_DEV
        nn.fs.FileSystem.Unmount(mountName);
#endif
    }

    public bool SaveString(string dataToSave, string fileName, int fileSizeInBytes)
    {
        if (!IsValidRequestSize(fileSizeInBytes)) return false;

        byte[] dataByteArray;
        using (MemoryStream stream = new MemoryStream(fileSizeInBytes)) // the stream size must be less than or equal to the save journal size
        {
            BinaryWriter binaryWriter = new BinaryWriter(stream);
            binaryWriter.Write(dataToSave);
            stream.Close();
            dataByteArray = stream.GetBuffer();
        }
#if (UNITY_SWITCH && !UNITY_EDITOR) || SWITCH_DEV
        return SaveInternalSwitch(dataByteArray, fileName);
#else
        return SaveInternalDefault(dataByteArray, fileName);
#endif
    }

    public bool SaveDataList<T>(List<T> saveDataList, string fileName, int fileSizeInBytes)
    {
        if (!IsValidRequestSize(fileSizeInBytes)) return false;

        byte[] dataByteArray;
        using (MemoryStream stream = new MemoryStream(fileSizeInBytes)) // the stream size must be less than or equal to the save journal size
        {
            try
            {
                dataByteArray = SerializeStream(saveDataList, stream);
            }
            catch(System.Exception e)
            {
                throw e;
            }
            finally
            {
                stream.Close();
            }
        }

#if (UNITY_SWITCH && !UNITY_EDITOR) || SWITCH_DEV
        return SaveInternalSwitch(dataByteArray, fileName);
#else
        return SaveInternalDefault(dataByteArray, fileName);
#endif
    }

    private bool SaveInternalDefault(byte[] dataByteArray, string fileName)
    {
        string filePath = saveDataPath + fileName;
        try
        {
            BinaryFormatter bf = new BinaryFormatter();
            FileStream file = System.IO.File.Create(filePath, dataByteArray.Length);
            file.Write(dataByteArray, 0, dataByteArray.Length);
            file.Close();
            return true;
        }
        catch (IOException)
        {
            return false;
        }
    }

    private bool SaveInternalSwitch(byte[] dataByteArray, string fileName)
    {
        bool result = false;
#if (UNITY_SWITCH && !UNITY_EDITOR) || SWITCH_DEV
        string filePath = saveDataPath + fileName;

        // This next line prevents the user from quitting the game while saving.
        // This is required for Nintendo Switch Guideline 0080
        SetExitRequestHandlerLocker(lockerName);
        // If you only ever save the entire file, it may be simpler just to delete the file and create a new one every time you save.
        // Most of the functions return an nn.Result which can be used for debugging purposes.
        result = true;
        result &= nn.fs.File.Delete(filePath).IsSuccess();
        result &= nn.fs.File.Create(filePath, dataByteArray.LongLength).IsSuccess(); //this makes a file the size of your save journal. You may want to make a file smaller than this.
        result &= nn.fs.File.Open(ref fileHandle, filePath, nn.fs.OpenFileMode.Write).IsSuccess();
        result &= nn.fs.File.Write(fileHandle, 0, dataByteArray, dataByteArray.LongLength, nn.fs.WriteOption.Flush).IsSuccess(); // Writes and flushes the write at the same time
        nn.fs.File.Close(fileHandle);
        result &= nn.fs.SaveData.Commit(mountName).IsSuccess(); //you must commit the changes.

        // End preventing the user from quitting the game while saving.
        RemoveExitREquestHandlerLocker(lockerName);
#endif
        return result;
    }


    public bool LoadString(ref string outputData, string fileName, int loadBufferSizeInBytes)
    {
        if (!IsValidRequestSize(loadBufferSizeInBytes)) return false;

        byte[] loadedData = null;
        if (!LoadData(ref loadedData, fileName, loadBufferSizeInBytes))
            return false;

        if (loadedData != null)
        {
            using (MemoryStream stream = new MemoryStream(loadedData))
            {
                BinaryReader reader = new BinaryReader(stream);
                outputData = reader.ReadString();
            }
        }
        return true;
    }

    public bool LoadList<T>(ref List<T> outputData, string fileName, int loadBufferSizeInBytes)
    {
        if (!IsValidRequestSize(loadBufferSizeInBytes)) return false;

        byte[] loadedData = null;
        if (!LoadData(ref loadedData, fileName, loadBufferSizeInBytes))
            return false;

        if(loadedData != null)
        {
            using (MemoryStream stream = new MemoryStream(loadedData))
            {
                bool success = false;
                try
                {
                    outputData = DeserializeStream<List<T>>(stream);
                }
                catch(System.Exception e)
                {
                    throw e;
                }
                finally
                {
                    stream.Close();
                }
                return success;
            }
        }
        return true;
    }

    public bool LoadData(ref byte[] loadedData, string fileName, int loadBufferSizeInBytes)
    {
		if (!IsValidRequestSize(loadBufferSizeInBytes))
		{
			Debug.Log("Not valid size");
			return false;
		}
#if (UNITY_SWITCH && !UNITY_EDITOR) || SWITCH_DEV
        if (!LoadInternalSwitch(ref loadedData, fileName, loadBufferSizeInBytes)) return false;
#else
		if (!LoadInternalDefault(ref loadedData, fileName, loadBufferSizeInBytes))
		{
			Debug.Log("Load Internal Default Failed");
			return false;
		}
#endif
			return true;
    }

    private bool LoadInternalSwitch(ref byte[] loadedData, string fileName, int loadBufferSizeInBytes)
    {
#if (UNITY_SWITCH && !UNITY_EDITOR) || SWITCH_DEV
        nn.fs.EntryType entryType = 0; //init to a dummy value (C# requirement)
        nn.fs.FileSystem.GetEntryType(ref entryType, saveDataPath);
        nn.Result result = nn.fs.File.Open(ref fileHandle, saveDataPath + fileName, nn.fs.OpenFileMode.Read);
        if (result.IsSuccess() == false)
        {
            return false;   // Could not open file. This can be used to detect if this is the first time a user has launched your game.
                            // (However, be sure you are not getting this error due to your file being locked by another process, etc.)
        }
        loadedData = new byte[loadBufferSizeInBytes];
        nn.fs.File.Read(fileHandle, 0, loadedData, loadBufferSizeInBytes);
        nn.fs.File.Close(fileHandle);
#endif
        return true;
    }

    private bool LoadInternalDefault(ref byte[] loadedData, string fileName, int loadBufferSizeInBytes)
    {
        string filePath = saveDataPath + fileName;
        try
        {
            if (System.IO.File.Exists(filePath))
            {
                loadedData = new byte[loadBufferSizeInBytes];
                BinaryFormatter bf = new BinaryFormatter();
                FileStream file = System.IO.File.Open(filePath, FileMode.Open);
                file.Read(loadedData, 0, loadBufferSizeInBytes);
                file.Close();
            }
            return true;
        }
        catch (IOException ex)
        {
			Debug.LogError(ex);
            return false;
        }
    }

    public bool DeleteFile(string fileName)
    {
#if (UNITY_SWITCH && !UNITY_EDITOR) || SWITCH_DEV
        if (!DeleteFileSwitch(fileName)) return false;
#else
        if (!DeleteFileDefault(fileName)) return false;
#endif
        return true;
    }

#if (UNITY_SWITCH && !UNITY_EDITOR) || SWITCH_DEV
    private bool DeleteFileSwitch(string fileName)
    {  
        nn.fs.EntryType entryType = 0; //init to a dummy value (C# requirement)
        nn.fs.FileSystem.GetEntryType(ref entryType, saveDataPath);
        nn.Result result = nn.fs.File.Delete(saveDataPath + fileName);
        return result.IsSuccess();
    }
#endif

    private bool DeleteFileDefault(string fileName)
    {
        string filePath = saveDataPath + fileName;
        try
        {
            if (System.IO.File.Exists(filePath))
            {
                System.IO.File.Delete(filePath);
            }
            return true;
        }
        catch (IOException)
        {
            return false;
        }
    }

    static public T DeserializeStream<T>(MemoryStream stream)
    {
        T outputData;
        try
        {
            BinaryFormatter binaryFormatter = new BinaryFormatter();
            outputData = (T)binaryFormatter.Deserialize(stream);
        }
        catch (System.Exception e)
        {
            throw e;
        }
        return outputData;
    }

    static public byte[] SerializeStream<T>(List<T> saveDataList, MemoryStream stream)
    {
        try
        {
            BinaryFormatter binFormatter = new BinaryFormatter();
            binFormatter.Serialize(stream, saveDataList);
            stream.Close();
            return stream.GetBuffer();
        }
        catch (System.Exception e)
        {
            throw e;
        }
    }

    public static int KBtoBytes(int kB)
    {
        return kB * 1024;
    }

    public bool IsValidRequestSize(int bytes)
    {
        Debug.Assert(bytes <= journalSaveDataSize);
        return bytes <= journalSaveDataSize;
    }

#if (UNITY_SWITCH && !UNITY_EDITOR) || SWITCH_DEV
    public void SetExitRequestHandlerLocker(string key)
    {
        bool wasLocked = exitRequestHandlerLocker.IsLocked();
        exitRequestHandlerLocker.SetLocker(key);
        if (!wasLocked)
        {
            UnityEngine.Switch.Notification.EnterExitRequestHandlingSection();
        }
    }

    public void RemoveExitREquestHandlerLocker(string key)
    {
        bool wasLocked = exitRequestHandlerLocker.IsLocked();
        exitRequestHandlerLocker.RemoveLocker(key);
        if (wasLocked && !exitRequestHandlerLocker.IsLocked())
        {
            UnityEngine.Switch.Notification.LeaveExitRequestHandlingSection();
        }
    }
#endif 

    public void OnApplicationQuit()
    {
#if (UNITY_SWITCH && !UNITY_EDITOR) || SWITCH_DEV
        PlayerPrefsSwitch.PlayerPrefsSwitch.Term();
#endif
        applicationExiting = true;
    }
}
