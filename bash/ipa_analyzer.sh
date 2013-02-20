#! /bin/bash

#
# Pre-requisite:
#  * unzip
#  * sqlite3 

prog=`basename $0`

TO_COPY_FILE=0  # 1: copy file; 0: move file

DB_FILE=apps.db
PLREADER=./plistreader
#IPA_REPO="/Volumes/My Passport/Device/iOS/Cracked_IPA"
#IPA_REPO=/Users/yongfeng/data/iPhone/IPA
IPA_REPO=/Users/yongfeng/data/yongfeng/apps_output_old
OUTPUT_REPO=/Users/yongfeng/data/yongfeng/apps_output

#IPA_REPO=/Users/yongfeng/data/yongfeng/apps
#OUTPUT_REPO=/Users/yongfeng/data/yongfeng/apps_output

#OUTPUT_REPO=/Users/yongfeng/tmp/ipa_organizer/OUTPUT
#IPA_REPO=/Users/yongfeng/tmp/ipa_organizer/OUTPUT
#IPA_REPO=/Users/yongfeng/Data/Device/iPhone/zoo/Apps
#PLREADER=/Users/yongfeng/tmp/ipa_organizer/plistreader
#PLREADER=./plreader.py

URL_PREF="http://itunes.apple.com/WebObjects/MZStore.woa/wa/viewSoftware?id="

WORKSPACE=/Users/yongfeng/tmp/ipa_organizer/tmp
mkdir -p $WORKSPACE


function createDatabase() {
    [ -f $DB_FILE ] && rm -f $DB_FILE

    sqlite3 $DB_FILE << END
CREATE TABLE app (
  app_id INTEGER PRIMARY KEY
, old_filename TEXT
, app_filename TEXT
, app_filesize TEXT
, ipa_payload_name TEXT
, ipa_genre TEXT
, bundle_id TEXT
, bundle_name TEXT
, bundle_display_name TEXT
, bundle_executable TEXT
, bundle_short_version TEXT
, bundle_version TEXT
, bundle_min_os_version TEXT
, app_type TEXT
, ipa_itunes_id INTEGER
, apptrackr_itunes_id INTEGER
, itunes_primary_genre TEXT
, remarks TEXT
);
END
}

function esc() {
    echo "$*" | sed "s/\'/\'\'/g"
}

function organizeFile() {
    ipa_file=$1
    new_name=$2
    app_type_name=$3

    output_dir=$OUTPUT_REPO

    mkdir -p "$output_dir/$app_type_name"
    if [ -f "$output_dir/$app_type_name/$new_name" ]; then
        echo "$prog: already exists: skip"
    else
        if [ $TO_COPY_FILE == 1 ]; then
            echo "$prog: copying as new file: $output_dir/$app_type_name/$new_name"
            cp -f "$ipa_file" "$output_dir/$app_type_name/$new_name"
        else
            echo "$prog: moving as: $output_dir/$app_type_name/$new_name"
            mv -f "$ipa_file" "$output_dir/$app_type_name/$new_name"
        fi
    fi
}

function getProp() {
    $PLREADER "$1" "$2" 2>/dev/null
}

function process() {
find "$IPA_REPO" -name *.ipa -type f | while read ipa_file; do
    #echo "************************************************"
    #echo "$ipa_file"
	old_name=`basename "$ipa_file"`
    echo "$prog: processing \`$old_name'..."
    filesize=`du -sh "$ipa_file" | cut -f1`
	unzip -q -o -d $WORKSPACE "$ipa_file" iTunesMetadata.plist Payload/*/Info.plist 2>/dev/null

    if [ -d $WORKSPACE/Payload ]; then
        payloadName=`ls $WORKSPACE/Payload | sed 's/\.app//'`
    else
        payloadName=
    fi

    infoPlist=`ls $WORKSPACE/Payload/*.app/Info.plist 2>/dev/null`

    _appType=0
    _appTypeName=
    
    if [ ! -z "infoPlist" ] && [ -f "$infoPlist" ]; then
        CFBundleDisplayName=$(getProp "$infoPlist" CFBundleDisplayName)
        CFBundleIdentifier=$(getProp "$infoPlist" CFBundleIdentifier)
        CFBundleVersion=$(getProp "$infoPlist" CFBundleVersion)
        CFBundleShortVersionString=$(getProp "$infoPlist" CFBundleShortVersionString)
        CFBundleName=$(getProp "$infoPlist" CFBundleName)
        CFBundleExecutable=$(getProp "$infoPlist" CFBundleExecutable)
        MinimumOSVersion=$(getProp "$infoPlist" MinimumOSVersion)
        UIDeviceFamily=$(getProp "$infoPlist" UIDeviceFamily)
        if [[ "$UIDeviceFamily" == *1* ]] && [[ "$UIDeviceFamily" == *2* ]]; then
            _appType=0
            _appTypeName=universal
        elif [[ "$UIDeviceFamily" == *1* ]]; then
            _appType=1
            _appTypeName=iphone
        elif [[ "$UIDeviceFamily" == *2* ]]; then
            _appType=2
            _appTypeName=ipad
        fi
    else
        CFBundleDisplayName=
        CFBundleIdentifier=
        CFBundleVersion=
        CFBundleShortVersionString=
        CFBundleName=
        CFBundleExecutable=
        MinimumOSVersion=
    fi

    metaPlist=$WORKSPACE/iTunesMetadata.plist
    if [ -f "$metaPlist" ]; then
        genre=$(getProp "$metaPlist" genre)
        itemId=$(getProp "$metaPlist" itemId)
    else
        genre=
        itemId=
    fi

    if [ ! -z $itemId ]; then
        url=$URL_PREF$itemId
    else
        url=
    fi

    # CFBundleDisplayName, CFBundleExecutable
    new_name="$old_name"

    code_name=$CFBundleExecutable
    [ -z "$code_name" ] && code_name=$payloadName

    n=$CFBundleDisplayName
    [ -z "$n" ] && n=$code_name

    [ ! -z "$n" ] && [ ! -z "$code_name" ] && [ "$n" != "$code_name" ] && n="${n}_($code_name)"

    if [ ! -z "$n" ]; then
        v=
        # version
        [ ! -z "$CFBundleShortVersionString" ] && v="v$CFBundleShortVersionString"

        [ ! -z "$v" ] && [ ! -z "$CFBundleVersion" ] && [ "$CFBundleShortVersionString" != "$CFBundleVersion" ] && v="${v}_($CFBundleVersion)"

        [ -z "$v" ] && [ ! -z "$CFBundleVersion" ] && v="v$CFBundleVersion"

        [ ! -z "$v" ] && n=${n}_$v

        # os version
        [ ! -z "$MinimumOSVersion" ] && n="${n}_os$MinimumOSVersion"

        # app type
        [ ! -z "_appTypeName" ] && n="${n}_${_appTypeName}"
        
        # id
        [ ! -z "$CFBundleIdentifier" ] && n="${n}_$CFBundleIdentifier"

        new_name="$n.ipa"
    fi

    organizeFile "$ipa_file" "$new_name" "$_appTypeName"

    db_old_filename=$(esc $old_name)
    db_app_filename=$(esc $new_name)
    db_app_filesize=$(esc $filesize)
    db_ipa_payload_name=$(esc $payloadName)
    db_ipa_genre=$(esc $genre)
    db_bundle_id=$(esc $CFBundleIdentifier)
    db_bundle_name=$(esc $CFBundleName)
    db_bundle_display_name=$(esc $CFBundleDisplayName)
    bundle_executable=$(esc $CFBundleExecutable)
    db_bundle_short_version=$(esc $CFBundleShortVersionString)
    db_bundle_version=$(esc $CFBundleVersion)
    db_bundle_min_os_version=$(esc $MinimumOSVersion)
    db_app_type=_appTypeName
    db_ipa_itunes_id=$(esc $itemId)

    sqlite3 $DB_FILE << END
INSERT INTO app (
old_filename, app_filename, app_filesize, ipa_payload_name, ipa_genre, 
bundle_id, bundle_name, bundle_display_name,
bundle_executable, bundle_short_version, bundle_version, bundle_min_os_version, 
app_type, ipa_itunes_id
) VALUES (
'$db_old_filename', '$db_app_filename', '$db_app_filesize', '$db_ipa_payload_name', '$db_ipa_genre',
'$db_bundle_id', '$db_bundle_name', '$db_bundle_display_name',
'$db_bundle_executable', '$db_bundle_short_version', '$db_bundle_version', '$db_bundle_min_os_version', 
'$db_app_type', '$db_ipa_itunes_id'
);
END

    rm -rf $WORKSPACE/*
done
}

createDatabase
process

