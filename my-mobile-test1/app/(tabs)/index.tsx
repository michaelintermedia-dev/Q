import { RecordingPresets, requestRecordingPermissionsAsync, useAudioPlayer, useAudioRecorder } from 'expo-audio';
import { useEffect, useState } from 'react';
import { ActivityIndicator, FlatList, KeyboardAvoidingView, Modal, Platform, Pressable, ScrollView, StyleSheet, Text, TextInput, View } from 'react-native';
//import '../../firebase';
import { apiGet, getApiUrl } from '../../api/client';
import { TokenService } from '../../auth/services/TokenService';
import DateTimePicker, { DateTimePickerAndroid } from '@react-native-community/datetimepicker';

interface AppointmentData {
    name: string;
    phone: string;
    appointmentDate: string;
    appointmentDurationMinutes: number;
    additionalText: string;
}

interface UploadResponse {
    appointment: AppointmentData;
    validation: {
        isSuccess: boolean;
        error: string;
    };
}

function formatDateForDisplay(isoString: string): string {
    try {
        const date = new Date(isoString);
        return date.toLocaleString();
    } catch {
        return isoString;
    }
}

function RecordingItem({ uri, index }: { uri: string; index: number }) {
    const player = useAudioPlayer(uri);
    const [uploading, setUploading] = useState(false);
    const [showForm, setShowForm] = useState(false);
    const [submitting, setSubmitting] = useState(false);
    const [appointment, setAppointment] = useState<AppointmentData | null>(null);
    const [validationError, setValidationError] = useState<string | null>(null);
    const [showDatePicker, setShowDatePicker] = useState(false);
    const [editingDateManually, setEditingDateManually] = useState(false);
    const [dateInputText, setDateInputText] = useState('');

    function updateField<K extends keyof AppointmentData>(field: K, value: AppointmentData[K]) {
        setAppointment(prev => prev ? { ...prev, [field]: value } : prev);
    }

    async function uploadRecording() {
        try {
            setUploading(true);
            console.log('[Upload] Starting upload:', uri);

            // Generate unique filename based on timestamp
            const now = new Date();
            const timestamp = now.toISOString().replace(/[-:]/g, '').replace('T', '_').split('.')[0];
            const filename = `recording_${timestamp}.m4a`;

            const formData = new FormData();
            if (Platform.OS === 'web') {
                const audioResponse = await fetch(uri);
                const blob = await audioResponse.blob();
                formData.append('file', blob, filename);
            } else {
                // @ts-ignore
                formData.append('file', {
                    uri: uri,
                    name: filename,
                    type: 'audio/m4a',
                });
            }

            const apiUrl = getApiUrl('/UploadAudio');
            const token = await TokenService.getToken();
            console.log('[Upload] Uploading to:', apiUrl);

            // Create abort controller for timeout
            const controller = new AbortController();
            const timeoutId = setTimeout(() => controller.abort(), 30000); // 30 second timeout

            try {
                const response = await fetch(apiUrl, {
                    method: 'POST',
                    headers: {
                        'Accept': 'application/json',
                        ...(token && { 'Authorization': `Bearer ${token}` }),
                    },
                    body: formData,
                    signal: controller.signal,
                });

                clearTimeout(timeoutId);

                if (response.ok) {
                    const result: UploadResponse = await response.json();
                    console.log('[Upload] Success:', result);

                    if (!result.validation.isSuccess) {
                        setValidationError(result.validation.error);
                    } else {
                        setValidationError(null);
                    }

                    setAppointment(result.appointment);
                    setShowForm(true);
                } else {
                    const errorText = await response.text();
                    console.error('[Upload] Failed', response.status, errorText);
                    alert(`Upload failed: ${response.status} - ${errorText}`);
                }
            } catch (fetchErr: any) {
                clearTimeout(timeoutId);
                if (fetchErr.name === 'AbortError') {
                    console.error('[Upload] Timeout');
                    alert('Upload timeout - cannot reach server at ' + apiUrl);
                } else {
                    throw fetchErr;
                }
            }
        } catch (err: any) {
            console.error('[Upload] Error', err);
            alert(`Upload error: ${err.message || err}`);
        } finally {
            setUploading(false);
        }
    }

    function handleDiscard() {
        console.log('[Appointment] Discarded');
        setShowForm(false);
        setAppointment(null);
        setValidationError(null);
        setShowDatePicker(false);
        setEditingDateManually(false);
        setDateInputText('');
    }

    async function handleConfirm() {
        if (!appointment) return;

        try {
            setSubmitting(true);
            const apiUrl = getApiUrl('/ConfirmAppointment');
            const token = await TokenService.getToken();
            console.log('[Appointment] Confirming:', appointment);

            const response = await fetch(apiUrl, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'Accept': 'application/json',
                    ...(token && { 'Authorization': `Bearer ${token}` }),
                },
                body: JSON.stringify(appointment),
            });

            if (response.ok) {
                console.log('[Appointment] Confirmed successfully');
                alert('Appointment confirmed!');
                setShowForm(false);
                setAppointment(null);
                setValidationError(null);
            } else {
                const errorText = await response.text();
                console.error('[Appointment] Confirm failed:', response.status, errorText);
                alert(`Failed to confirm: ${response.status} - ${errorText}`);
            }
        } catch (err: any) {
            console.error('[Appointment] Confirm error:', err);
            alert(`Error confirming appointment: ${err.message || err}`);
        } finally {
            setSubmitting(false);
        }
    }

    return (
        <View style={styles.recordingItemContainer}>
            <Pressable onPress={() => {
                player.seekTo(0);
                player.play();
            }} style={styles.playbackArea}>
                <View style={styles.recordingItem}>
                    <Text>Recording {index + 1}: {uri.split('/').pop()}</Text>
                </View>
            </Pressable>
            <Pressable onPress={uploadRecording} style={styles.saveButton} disabled={uploading}>
                {uploading ? (
                    <ActivityIndicator size="small" color="#fff" />
                ) : (
                    <Text style={styles.saveButtonText}>Save</Text>
                )}
            </Pressable>

            <Modal
                visible={showForm}
                animationType="slide"
                transparent={true}
                onRequestClose={handleDiscard}
            >
                <KeyboardAvoidingView
                    style={styles.modalOverlay}
                    behavior={Platform.OS === 'ios' ? 'padding' : undefined}
                >
                    <View style={styles.modalContent}>
                        <ScrollView showsVerticalScrollIndicator={false}>
                            <Text style={styles.modalTitle}>Appointment Details</Text>

                            {validationError ? (
                                <View style={styles.validationError}>
                                    <Text style={styles.validationErrorText}>⚠️ {validationError}</Text>
                                </View>
                            ) : null}

                            <Text style={styles.fieldLabel}>Name</Text>
                            <TextInput
                                style={styles.fieldInput}
                                value={appointment?.name ?? ''}
                                onChangeText={(text) => updateField('name', text)}
                                placeholder="Name"
                            />

                            <Text style={styles.fieldLabel}>Phone</Text>
                            <TextInput
                                style={styles.fieldInput}
                                value={appointment?.phone ?? ''}
                                onChangeText={(text) => updateField('phone', text)}
                                placeholder="Phone"
                                keyboardType="phone-pad"
                            />

                            <Text style={styles.fieldLabel}>Appointment Date</Text>
                            {editingDateManually ? (
                                <View style={styles.dateRow}>
                                    <TextInput
                                        style={[styles.fieldInput, styles.dateTextInput]}
                                        value={dateInputText}
                                        onChangeText={setDateInputText}
                                        onBlur={() => {
                                            const parsed = new Date(dateInputText);
                                            if (!isNaN(parsed.getTime())) {
                                                updateField('appointmentDate', parsed.toISOString());
                                            }
                                        }}
                                        placeholder="e.g. Jan 25, 2026 1:40 PM"
                                    />
                                    <Pressable
                                        onPress={() => {
                                            const parsed = new Date(dateInputText);
                                            if (!isNaN(parsed.getTime())) {
                                                updateField('appointmentDate', parsed.toISOString());
                                            }
                                            setEditingDateManually(false);
                                        }}
                                        style={styles.dateToggleButton}
                                    >
                                        <Text style={styles.dateToggleText}>📅</Text>
                                    </Pressable>
                                </View>
                            ) : (
                                <View>
                                    <Pressable
                                        onPress={async () => {
                                            if (Platform.OS === 'android') {
                                                try {
                                                    const currentDate = appointment?.appointmentDate ? new Date(appointment.appointmentDate) : new Date();
                                                    const { action: dateAction, year, month, day } = await DateTimePickerAndroid.open({
                                                        value: currentDate,
                                                        mode: 'date',
                                                    }) as any;
                                                    if (dateAction === 'dismissedAction') return;
                                                    const { action: timeAction, hours, minutes } = await DateTimePickerAndroid.open({
                                                        value: currentDate,
                                                        mode: 'time',
                                                        is24Hour: true,
                                                    }) as any;
                                                    if (timeAction === 'dismissedAction') return;
                                                    const selected = new Date(year, month, day, hours, minutes);
                                                    updateField('appointmentDate', selected.toISOString());
                                                } catch (e) {
                                                    console.error('[DatePicker] Android error:', e);
                                                }
                                            } else if (Platform.OS === 'web') {
                                                setShowDatePicker(true);
                                            } else {
                                                setShowDatePicker(!showDatePicker);
                                            }
                                        }}
                                        style={styles.datePickerTrigger}
                                    >
                                        <Text style={appointment?.appointmentDate ? styles.datePickerText : styles.datePickerPlaceholder}>
                                            {appointment?.appointmentDate ? formatDateForDisplay(appointment.appointmentDate) : 'Select date & time'}
                                        </Text>
                                        <Pressable
                                            onPress={() => {
                                                setDateInputText(
                                                    appointment?.appointmentDate
                                                        ? formatDateForDisplay(appointment.appointmentDate)
                                                        : ''
                                                );
                                                setEditingDateManually(true);
                                            }}
                                            style={styles.dateToggleButton}
                                        >
                                            <Text style={styles.dateToggleText}>✏️</Text>
                                        </Pressable>
                                    </Pressable>
                                    {showDatePicker && Platform.OS === 'ios' && (
                                        <>
                                            <DateTimePicker
                                                value={appointment?.appointmentDate ? new Date(appointment.appointmentDate) : new Date()}
                                                mode="datetime"
                                                display="spinner"
                                                onChange={(_event: any, selectedDate?: Date) => {
                                                    if (selectedDate) {
                                                        updateField('appointmentDate', selectedDate.toISOString());
                                                    }
                                                }}
                                            />
                                            <Pressable
                                                onPress={() => setShowDatePicker(false)}
                                                style={styles.datePickerDone}
                                            >
                                                <Text style={styles.datePickerDoneText}>Done</Text>
                                            </Pressable>
                                        </>
                                    )}
                                    {showDatePicker && Platform.OS === 'web' && (
                                        <TextInput
                                            style={styles.fieldInput}
                                            value={appointment?.appointmentDate ? appointment.appointmentDate.slice(0, 16) : ''}
                                            onChangeText={(text) => {
                                                if (text) {
                                                    updateField('appointmentDate', new Date(text).toISOString());
                                                }
                                            }}
                                            placeholder="YYYY-MM-DDTHH:MM"
                                        />
                                    )}
                                </View>
                            )}

                            <Text style={styles.fieldLabel}>Duration (minutes)</Text>
                            <TextInput
                                style={styles.fieldInput}
                                value={appointment?.appointmentDurationMinutes?.toString() ?? ''}
                                onChangeText={(text) => {
                                    const num = parseInt(text, 10);
                                    updateField('appointmentDurationMinutes', isNaN(num) ? 0 : num);
                                }}
                                placeholder="Duration in minutes"
                                keyboardType="numeric"
                            />

                            <Text style={styles.fieldLabel}>Additional Notes</Text>
                            <TextInput
                                style={[styles.fieldInput, styles.fieldInputMultiline]}
                                value={appointment?.additionalText ?? ''}
                                onChangeText={(text) => updateField('additionalText', text)}
                                placeholder="Additional notes"
                                multiline
                                numberOfLines={3}
                            />

                            <View style={styles.modalButtons}>
                                <Pressable onPress={handleDiscard} style={styles.discardButton} disabled={submitting}>
                                    <Text style={styles.discardButtonText}>Discard</Text>
                                </Pressable>
                                <Pressable onPress={handleConfirm} style={styles.confirmButton} disabled={submitting}>
                                    {submitting ? (
                                        <ActivityIndicator size="small" color="#fff" />
                                    ) : (
                                        <Text style={styles.confirmButtonText}>OK</Text>
                                    )}
                                </Pressable>
                            </View>
                        </ScrollView>
                    </View>
                </KeyboardAvoidingView>
            </Modal>
        </View>
    );
}

export default function Index() {
const recorder = useAudioRecorder(RecordingPresets.HIGH_QUALITY);
const [recordings, setRecordings] = useState<string[]>([]);
const [isRecording, setIsRecording] = useState(false);
const [cmsContent, setCmsContent] = useState<Record<string, any> | null>(null);
const [cmsLoading, setCmsLoading] = useState(true);

useEffect(() => {
    async function fetchCmsContent() {
        try {
            setCmsLoading(true);
            const data = await apiGet<Record<string, any>>('/cms/content/contenttest1');
            console.log('[CMS] Fetched content:', data);
            setCmsContent(data);
        } catch (err: any) {
            console.error('[CMS] Fetch error:', err);
        } finally {
            setCmsLoading(false);
        }
    }
    fetchCmsContent();
}, []);

    async function startRecording() {
        try {
            const permission = await requestRecordingPermissionsAsync();
            if (permission.status !== 'granted') {
                console.log("Permission not granted");
                return;
            }

            await recorder.prepareToRecordAsync();
            recorder.record();
            setIsRecording(true);
            console.log('Recording started');
        } catch (err) {
            console.error('Failed to start recording', err);
        }
    }

    async function stopRecording() {
        if (!isRecording) return;

        console.log('Stopping recording..');
        await recorder.stop();
        setIsRecording(false);

        // We get the URI from the recorder instance
        const uri = recorder.uri;
        console.log('Recording stopped and stored at', uri);
        if (uri) {
            setRecordings(prev => [...prev, uri]);
        }
    }

    async function checkServerLiveness() {
        try {
            const apiUrl = getApiUrl('/healthz/live');
            console.log('[Server] Checking liveness:', apiUrl);

            const controller = new AbortController();
            const timeoutId = setTimeout(() => controller.abort(), 5000); // 5 second timeout

            const response = await fetch(apiUrl, {
                method: 'GET',
                signal: controller.signal,
            });

            clearTimeout(timeoutId);

            if (response.ok) {
                const result = await response.text();
                console.log('[Server] Alive:', result);
                alert(`? Server is alive!\n${result}`);
            } else {
                console.error('[Server] Check failed:', response.status);
                alert(`? Server returned: ${response.status}`);
            }
        } catch (err: any) {
            console.error('[Server] Error:', err);
            if (err.name === 'AbortError') {
                alert('? Server timeout - cannot reach server');
            } else {
                alert(`? Server error: ${err.message || err}`);
            }
        }
    }

    return (
        <View style={styles.container}>
            <Text style={styles.title}>Audio Recorder</Text>

            {cmsLoading ? (
                <ActivityIndicator size="small" color="#007AFF" style={styles.cmsLoading} />
            ) : cmsContent ? (
                <View style={styles.cmsContainer}>
                    {Object.entries(cmsContent).map(([key, value]) => (
                        <Text key={key} style={styles.cmsText}>
                            {typeof value === 'string' ? value : JSON.stringify(value)}
                        </Text>
                    ))}
                </View>
            ) : null}

            <View style={styles.buttonContainer}>
                <Pressable
                    onPressIn={startRecording}
                    onPressOut={stopRecording}
                    style={({ pressed }) => [
                        styles.recordButton,
                        pressed ? styles.recordButtonPressed : null
                    ]}
                >
                    <Text style={styles.recordButtonText}>
                        {isRecording ? 'Recording...' : 'Hold to Record'}
                    </Text>
                </Pressable>
            </View>

            <FlatList
                data={recordings}
                keyExtractor={(item, index) => index.toString()}
                renderItem={({ item, index }) => (
                    <RecordingItem uri={item} index={index} />
                )}
                style={styles.list}
            />

            <Pressable onPress={checkServerLiveness} style={styles.livenessButton}>
                <Text style={styles.livenessButtonText}>Server Liveness Probe</Text>
            </Pressable>
        </View>
    );
}

const styles = StyleSheet.create({
    container: {
        flex: 1,
        alignItems: 'center',
        justifyContent: 'center',
        paddingTop: 50,
        backgroundColor: '#fff',
    },
    title: {
        fontSize: 24,
        fontWeight: 'bold',
        marginBottom: 20,
    },
    buttonContainer: {
        marginBottom: 20,
    },
    recordButton: {
        backgroundColor: '#007AFF',
        paddingVertical: 15,
        paddingHorizontal: 30,
        borderRadius: 25,
        minWidth: 200,
        alignItems: 'center',
    },
    recordButtonPressed: {
        backgroundColor: '#FF3B30',
    },
    recordButtonText: {
        color: '#fff',
        fontSize: 18,
        fontWeight: '600',
    },
    list: {
        flex: 1,
        width: '100%',
        paddingHorizontal: 20,
    },
    recordingItem: {
        padding: 15,
    },
    recordingItemContainer: {
        flexDirection: 'row',
        alignItems: 'center',
        borderBottomWidth: 1,
        borderBottomColor: '#eee',
        paddingRight: 15,
    },
    playbackArea: {
        flex: 1,
    },
    saveButton: {
        backgroundColor: '#34C759',
        paddingVertical: 8,
        paddingHorizontal: 15,
        borderRadius: 15,
    },
    saveButtonText: {
        color: '#fff',
        fontWeight: 'bold',
    },
    livenessButton: {
        backgroundColor: '#5856D6',
        paddingVertical: 12,
        paddingHorizontal: 20,
        borderRadius: 20,
        marginBottom: 20,
        marginTop: 10,
    },
    livenessButtonText: {
        color: '#fff',
        fontSize: 16,
        fontWeight: '600',
    },
    cmsLoading: {
        marginBottom: 15,
    },
    cmsContainer: {
        backgroundColor: '#F0F4FF',
        borderRadius: 12,
        padding: 15,
        marginHorizontal: 20,
        marginBottom: 15,
        width: '90%',
    },
    cmsText: {
        fontSize: 14,
        color: '#333',
        marginBottom: 4,
    },
    modalOverlay: {
        flex: 1,
        backgroundColor: 'rgba(0,0,0,0.5)',
        justifyContent: 'center',
        padding: 20,
    },
    modalContent: {
        backgroundColor: '#fff',
        borderRadius: 16,
        padding: 24,
        maxHeight: '90%',
    },
    modalTitle: {
        fontSize: 22,
        fontWeight: 'bold',
        marginBottom: 16,
        textAlign: 'center',
        color: '#000',
    },
    validationError: {
        backgroundColor: '#FFF3CD',
        borderRadius: 8,
        padding: 12,
        marginBottom: 16,
        borderWidth: 1,
        borderColor: '#FFCC02',
    },
    validationErrorText: {
        fontSize: 14,
        color: '#856404',
    },
    fieldLabel: {
        fontSize: 14,
        fontWeight: '600',
        color: '#333',
        marginBottom: 4,
        marginTop: 12,
    },
    fieldInput: {
        borderWidth: 1,
        borderColor: '#ccc',
        borderRadius: 10,
        paddingHorizontal: 14,
        paddingVertical: 10,
        fontSize: 16,
        backgroundColor: '#f9f9f9',
        color: '#000',
    },
    fieldInputMultiline: {
        minHeight: 80,
        textAlignVertical: 'top',
    },
    dateRow: {
        flexDirection: 'row',
        alignItems: 'center',
        gap: 8,
    },
    dateTextInput: {
        flex: 1,
    },
    datePickerTrigger: {
        flexDirection: 'row',
        alignItems: 'center',
        borderWidth: 1,
        borderColor: '#ccc',
        borderRadius: 10,
        paddingHorizontal: 14,
        paddingVertical: 12,
        backgroundColor: '#f9f9f9',
    },
    datePickerText: {
        flex: 1,
        fontSize: 16,
        color: '#000',
    },
    datePickerPlaceholder: {
        flex: 1,
        fontSize: 16,
        color: '#999',
    },
    dateToggleButton: {
        paddingHorizontal: 8,
        paddingVertical: 4,
    },
    dateToggleText: {
        fontSize: 20,
    },
    datePickerDone: {
        alignSelf: 'flex-end',
        marginTop: 8,
        paddingVertical: 6,
        paddingHorizontal: 16,
        backgroundColor: '#007AFF',
        borderRadius: 8,
    },
    datePickerDoneText: {
        color: '#fff',
        fontSize: 14,
        fontWeight: '600',
    },
    modalButtons: {
        flexDirection: 'row',
        justifyContent: 'space-between',
        marginTop: 24,
        gap: 12,
    },
    discardButton: {
        flex: 1,
        backgroundColor: '#FF3B30',
        paddingVertical: 14,
        borderRadius: 12,
        alignItems: 'center',
    },
    discardButtonText: {
        color: '#fff',
        fontSize: 16,
        fontWeight: '600',
    },
    confirmButton: {
        flex: 1,
        backgroundColor: '#34C759',
        paddingVertical: 14,
        borderRadius: 12,
        alignItems: 'center',
    },
    confirmButtonText: {
        color: '#fff',
        fontSize: 16,
        fontWeight: '600',
    },
});
