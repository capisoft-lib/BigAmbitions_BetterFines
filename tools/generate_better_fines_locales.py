#!/usr/bin/env python3
"""Generate BetterFines locale JSON files for all 22 game languages."""

from __future__ import annotations

import json
from pathlib import Path

OUT = Path(__file__).resolve().parents[1] / "Locales"

# SMS templates use {vehicleTypeName}, {hour}, {minute}, {day}, {amount}
LOCALES: dict[str, dict[str, str]] = {
    "en": {
        "betterfines_options_header": "Better Fines",
        "betterfines_options_speeding_enabled": "Speeding fines",
        "betterfines_options_speeding_fine": "Fine value ($)",
        "betterfines_options_speeding_min_delay": "Min delay (sec)",
        "betterfines_options_speeding_over_limit": "Over limit (km/h)",
        "betterfines_options_speeding_trigger_delay": "Trigger delay (sec)",
        "betterfines_options_red_light_enabled": "Red light fines",
        "betterfines_options_red_light_fine": "Fine value ($)",
        "betterfines_options_red_light_min_delay": "Min delay (sec)",
        "betterfines_options_red_light_min_speed": "Min speed (km/h)",
        "betterfines_options_red_light_orange": "Orange traffic light fine",
        "betterfines_warning_over_speed_limit": "warning! over speed limit",
        "betterfines_options_value_dollars": "${value}",
        "betterfines_options_value_seconds": "{value} sec",
        "betterfines_options_value_kmh": "{value} km/h",
        "betterfines:sms_government_speeding_ticket": (
            "<b>Notice of Speeding Violation</b><br><br>Dear Sir or Madam:<br><br>"
            "We are writing to inform you that your motor vehicle of type <b>{vehicleTypeName}</b> "
            "was recorded exceeding the speed limit at {hour}:{minute}, Day {day}.<br><br>"
            "You have been automatically charged a fine of <b>${amount}.00</b> from your primary bank account.<br><br>"
            "Sincerely,<br>The New York City Department of Finance"
        ),
        "betterfines:sms_government_red_light_ticket": (
            "<b>Notice of Traffic Violation</b><br><br>Dear Sir or Madam:<br><br>"
            "We are writing to inform you that your motor vehicle of type <b>{vehicleTypeName}</b> "
            "was recorded running a red light at {hour}:{minute}, Day {day}.<br><br>"
            "You have been automatically charged a fine of <b>${amount}.00</b> from your primary bank account.<br><br>"
            "Sincerely,<br>The New York City Department of Finance"
        ),
    },
    "fr": {
        "betterfines_options_header": "Better Fines",
        "betterfines_options_speeding_enabled": "Amendes excès de vitesse",
        "betterfines_options_speeding_fine": "Montant de l'amende ($)",
        "betterfines_options_speeding_min_delay": "Délai minimum (sec)",
        "betterfines_options_speeding_over_limit": "Dépassement toléré (km/h)",
        "betterfines_options_speeding_trigger_delay": "Délai avant amende (sec)",
        "betterfines_options_red_light_enabled": "Amendes feu rouge",
        "betterfines_options_red_light_fine": "Montant de l'amende ($)",
        "betterfines_options_red_light_min_delay": "Délai minimum (sec)",
        "betterfines_options_red_light_min_speed": "Vitesse minimum (km/h)",
        "betterfines_options_red_light_orange": "Amende feu orange",
        "betterfines_warning_over_speed_limit": "attention ! dépassement de vitesse",
        "betterfines_options_value_dollars": "${value}",
        "betterfines_options_value_seconds": "{value} sec",
        "betterfines_options_value_kmh": "{value} km/h",
        "betterfines:sms_government_speeding_ticket": (
            "<b>Avis d'infraction pour excès de vitesse</b><br><br>Madame, Monsieur,<br><br>"
            "Nous vous informons que votre véhicule de type <b>{vehicleTypeName}</b> "
            "a été enregistré en excès de vitesse à {hour}:{minute}, jour {day}.<br><br>"
            "Un prélèvement automatique de <b>{amount} $</b> a été effectué sur votre compte bancaire principal.<br><br>"
            "Cordialement,<br>Le Département des Finances de la Ville de New York"
        ),
        "betterfines:sms_government_red_light_ticket": (
            "<b>Avis d'infraction routière</b><br><br>Madame, Monsieur,<br><br>"
            "Nous vous informons que votre véhicule de type <b>{vehicleTypeName}</b> "
            "a été enregistré franchissant un feu rouge à {hour}:{minute}, jour {day}.<br><br>"
            "Un prélèvement automatique de <b>{amount} $</b> a été effectué sur votre compte bancaire principal.<br><br>"
            "Cordialement,<br>Le Département des Finances de la Ville de New York"
        ),
    },
    "de": {
        "betterfines_options_header": "Better Fines",
        "betterfines_options_speeding_enabled": "Bußgelder für Geschwindigkeitsüberschreitung",
        "betterfines_options_speeding_fine": "Bußgeldbetrag ($)",
        "betterfines_options_speeding_min_delay": "Mindestabstand (Sek.)",
        "betterfines_options_speeding_over_limit": "Toleranz (km/h)",
        "betterfines_options_speeding_trigger_delay": "Auslöseverzögerung (Sek.)",
        "betterfines_options_red_light_enabled": "Bußgelder bei Rotlicht",
        "betterfines_options_red_light_fine": "Bußgeldbetrag ($)",
        "betterfines_options_red_light_min_delay": "Mindestabstand (Sek.)",
        "betterfines_options_red_light_min_speed": "Mindestgeschwindigkeit (km/h)",
        "betterfines_options_red_light_orange": "Bußgeld bei Gelblicht",
        "betterfines_warning_over_speed_limit": "Achtung! Geschwindigkeitsüberschreitung",
        "betterfines:sms_government_speeding_ticket": (
            "<b>Mitteilung über Geschwindigkeitsverstoß</b><br><br>Sehr geehrte Damen und Herren,<br><br>"
            "wir möchten Sie darüber informieren, dass Ihr Kraftfahrzeug des Typs <b>{vehicleTypeName}</b> "
            "am Tag {day} um {hour}:{minute} mit überschrittener Höchstgeschwindigkeit erfasst wurde.<br><br>"
            "Von Ihrem primären Bankkonto wurde automatisch ein Bußgeld in Höhe von <b>${amount}.00</b> abgebucht.<br><br>"
            "Mit freundlichen Grüßen<br>The New York City Department of Finance"
        ),
        "betterfines:sms_government_red_light_ticket": (
            "<b>Mitteilung über Verkehrsverstoß</b><br><br>Sehr geehrte Damen und Herren,<br><br>"
            "wir möchten Sie darüber informieren, dass Ihr Kraftfahrzeug des Typs <b>{vehicleTypeName}</b> "
            "am Tag {day} um {hour}:{minute} bei Rot über die Kreuzung gefahren ist.<br><br>"
            "Von Ihrem primären Bankkonto wurde automatisch ein Bußgeld in Höhe von <b>${amount}.00</b> abgebucht.<br><br>"
            "Mit freundlichen Grüßen<br>The New York City Department of Finance"
        ),
    },
    "es": {
        "betterfines_options_header": "Better Fines",
        "betterfines_options_speeding_enabled": "Multas por exceso de velocidad",
        "betterfines_options_speeding_fine": "Importe de la multa ($)",
        "betterfines_options_speeding_min_delay": "Retraso mínimo (seg)",
        "betterfines_options_speeding_over_limit": "Margen permitido (km/h)",
        "betterfines_options_speeding_trigger_delay": "Retraso de activación (seg)",
        "betterfines_options_red_light_enabled": "Multas por semáforo en rojo",
        "betterfines_options_red_light_fine": "Importe de la multa ($)",
        "betterfines_options_red_light_min_delay": "Retraso mínimo (seg)",
        "betterfines_options_red_light_min_speed": "Velocidad mínima (km/h)",
        "betterfines_options_red_light_orange": "Multa por semáforo en ámbar",
        "betterfines_warning_over_speed_limit": "¡aviso! exceso de velocidad",
        "betterfines:sms_government_speeding_ticket": (
            "<b>Aviso de infracción por exceso de velocidad</b><br><br>Estimado señor o señora:<br><br>"
            "Le informamos de que su vehículo de tipo <b>{vehicleTypeName}</b> "
            "fue registrado superando el límite de velocidad a las {hour}:{minute}, día {day}.<br><br>"
            "Se le ha cargado automáticamente una multa de <b>${amount}.00</b> en su cuenta bancaria principal.<br><br>"
            "Atentamente,<br>El Departamento de Finanzas de la Ciudad de Nueva York"
        ),
        "betterfines:sms_government_red_light_ticket": (
            "<b>Aviso de infracción de tráfico</b><br><br>Estimado señor o señora:<br><br>"
            "Le informamos de que su vehículo de tipo <b>{vehicleTypeName}</b> "
            "fue registrado pasando un semáforo en rojo a las {hour}:{minute}, día {day}.<br><br>"
            "Se le ha cargado automáticamente una multa de <b>${amount}.00</b> en su cuenta bancaria principal.<br><br>"
            "Atentamente,<br>El Departamento de Finanzas de la Ciudad de Nueva York"
        ),
    },
    "it": {
        "betterfines_options_header": "Better Fines",
        "betterfines_options_speeding_enabled": "Multe per eccesso di velocità",
        "betterfines_options_speeding_fine": "Importo della multa ($)",
        "betterfines_options_speeding_min_delay": "Ritardo minimo (sec)",
        "betterfines_options_speeding_over_limit": "Tolleranza (km/h)",
        "betterfines_options_speeding_trigger_delay": "Ritardo di attivazione (sec)",
        "betterfines_options_red_light_enabled": "Multe per semaforo rosso",
        "betterfines_options_red_light_fine": "Importo della multa ($)",
        "betterfines_options_red_light_min_delay": "Ritardo minimo (sec)",
        "betterfines_options_red_light_min_speed": "Velocità minima (km/h)",
        "betterfines_options_red_light_orange": "Multa per semaforo arancione",
        "betterfines_warning_over_speed_limit": "attenzione! eccesso di velocità",
        "betterfines:sms_government_speeding_ticket": (
            "<b>Avviso di infrazione per eccesso di velocità</b><br><br>Gentile signore o signora,<br><br>"
            "La informiamo che il suo veicolo di tipo <b>{vehicleTypeName}</b> "
            "è stato registrato in eccesso di velocità alle {hour}:{minute}, giorno {day}.<br><br>"
            "Le è stato addebitato automaticamente un importo di <b>${amount}.00</b> sul suo conto bancario principale.<br><br>"
            "Cordiali saluti,<br>Il Dipartimento delle Finanze della Città di New York"
        ),
        "betterfines:sms_government_red_light_ticket": (
            "<b>Avviso di infrazione stradale</b><br><br>Gentile signore o signora,<br><br>"
            "La informiamo che il suo veicolo di tipo <b>{vehicleTypeName}</b> "
            "è stato registrato mentre passava con il semaforo rosso alle {hour}:{minute}, giorno {day}.<br><br>"
            "Le è stato addebitato automaticamente un importo di <b>${amount}.00</b> sul suo conto bancario principale.<br><br>"
            "Cordiali saluti,<br>Il Dipartimento delle Finanze della Città di New York"
        ),
    },
    "pt-BR": {
        "betterfines_options_header": "Better Fines",
        "betterfines_options_speeding_enabled": "Multas por excesso de velocidade",
        "betterfines_options_speeding_fine": "Valor da multa ($)",
        "betterfines_options_speeding_min_delay": "Intervalo mínimo (seg)",
        "betterfines_options_speeding_over_limit": "Margem permitida (km/h)",
        "betterfines_options_speeding_trigger_delay": "Atraso de acionamento (seg)",
        "betterfines_options_red_light_enabled": "Multas por sinal vermelho",
        "betterfines_options_red_light_fine": "Valor da multa ($)",
        "betterfines_options_red_light_min_delay": "Intervalo mínimo (seg)",
        "betterfines_options_red_light_min_speed": "Velocidade mínima (km/h)",
        "betterfines_options_red_light_orange": "Multa por sinal amarelo",
        "betterfines_warning_over_speed_limit": "aviso! excesso de velocidade",
        "betterfines:sms_government_speeding_ticket": (
            "<b>Notificação de infração por excesso de velocidade</b><br><br>Prezado(a) senhor(a),<br><br>"
            "Informamos que seu veículo do tipo <b>{vehicleTypeName}</b> "
            "foi registrado acima do limite de velocidade às {hour}:{minute}, dia {day}.<br><br>"
            "Foi debitado automaticamente em sua conta bancária principal o valor de <b>${amount}.00</b>.<br><br>"
            "Atenciosamente,<br>Departamento de Finanças da Cidade de Nova York"
        ),
        "betterfines:sms_government_red_light_ticket": (
            "<b>Notificação de infração de trânsito</b><br><br>Prezado(a) senhor(a),<br><br>"
            "Informamos que seu veículo do tipo <b>{vehicleTypeName}</b> "
            "foi registrado avançando o sinal vermelho às {hour}:{minute}, dia {day}.<br><br>"
            "Foi debitado automaticamente em sua conta bancária principal o valor de <b>${amount}.00</b>.<br><br>"
            "Atenciosamente,<br>Departamento de Finanças da Cidade de Nova York"
        ),
    },
    "ru": {
        "betterfines_options_header": "Better Fines",
        "betterfines_options_speeding_enabled": "Штрафы за превышение скорости",
        "betterfines_options_speeding_fine": "Сумма штрафа ($)",
        "betterfines_options_speeding_min_delay": "Мин. интервал (сек)",
        "betterfines_options_speeding_over_limit": "Допуск (км/ч)",
        "betterfines_options_speeding_trigger_delay": "Задержка срабатывания (сек)",
        "betterfines_options_red_light_enabled": "Штрафы за красный свет",
        "betterfines_options_red_light_fine": "Сумма штрафа ($)",
        "betterfines_options_red_light_min_delay": "Мин. интервал (сек)",
        "betterfines_options_red_light_min_speed": "Мин. скорость (км/ч)",
        "betterfines_options_red_light_orange": "Штраф за жёлтый свет",
        "betterfines_warning_over_speed_limit": "внимание! превышение скорости",
        "betterfines:sms_government_speeding_ticket": (
            "<b>Уведомление о превышении скорости</b><br><br>Уважаемый(ая) господин(жа),<br><br>"
            "Сообщаем, что ваш автомобиль типа <b>{vehicleTypeName}</b> "
            "был зафиксирован с превышением скорости в {hour}:{minute}, день {day}.<br><br>"
            "С вашего основного банковского счёта автоматически списан штраф в размере <b>${amount}.00</b>.<br><br>"
            "С уважением,<br>Департамент финансов города Нью-Йорк"
        ),
        "betterfines:sms_government_red_light_ticket": (
            "<b>Уведомление о нарушении ПДД</b><br><br>Уважаемый(ая) господин(жа),<br><br>"
            "Сообщаем, что ваш автомобиль типа <b>{vehicleTypeName}</b> "
            "был зафиксирован при проезде на красный свет в {hour}:{minute}, день {day}.<br><br>"
            "С вашего основного банковского счёта автоматически списан штраф в размере <b>${amount}.00</b>.<br><br>"
            "С уважением,<br>Департамент финансов города Нью-Йорк"
        ),
    },
    "pl": {
        "betterfines_options_header": "Better Fines",
        "betterfines_options_speeding_enabled": "Mandaty za przekroczenie prędkości",
        "betterfines_options_speeding_fine": "Kwota mandatu ($)",
        "betterfines_options_speeding_min_delay": "Min. odstęp (sek)",
        "betterfines_options_speeding_over_limit": "Tolerancja (km/h)",
        "betterfines_options_speeding_trigger_delay": "Opóźnienie wyzwalania (sek)",
        "betterfines_options_red_light_enabled": "Mandaty za czerwone światło",
        "betterfines_options_red_light_fine": "Kwota mandatu ($)",
        "betterfines_options_red_light_min_delay": "Min. odstęp (sek)",
        "betterfines_options_red_light_min_speed": "Min. prędkość (km/h)",
        "betterfines_options_red_light_orange": "Mandat za pomarańczowe światło",
        "betterfines_warning_over_speed_limit": "uwaga! przekroczenie prędkości",
        "betterfines:sms_government_speeding_ticket": (
            "<b>Zawiadomienie o przekroczeniu prędkości</b><br><br>Szanowny Panie / Szanowna Pani,<br><br>"
            "Informujemy, że pojazd typu <b>{vehicleTypeName}</b> "
            "został zarejestrowany z przekroczeniem dozwolonej prędkości o {hour}:{minute}, dzień {day}.<br><br>"
            "Z Państwa głównego konta bankowego automatycznie pobrano mandat w wysokości <b>${amount}.00</b>.<br><br>"
            "Z poważaniem,<br>Departament Finansów Miasta Nowy Jork"
        ),
        "betterfines:sms_government_red_light_ticket": (
            "<b>Zawiadomienie o wykroczeniu drogowym</b><br><br>Szanowny Panie / Szanowna Pani,<br><br>"
            "Informujemy, że pojazd typu <b>{vehicleTypeName}</b> "
            "został zarejestrowany przy przejeździe na czerwonym świetle o {hour}:{minute}, dzień {day}.<br><br>"
            "Z Państwa głównego konta bankowego automatycznie pobrano mandat w wysokości <b>${amount}.00</b>.<br><br>"
            "Z poważaniem,<br>Departament Finansów Miasta Nowy Jork"
        ),
    },
    "nl": {
        "betterfines_options_header": "Better Fines",
        "betterfines_options_speeding_enabled": "Boetes voor te hard rijden",
        "betterfines_options_speeding_fine": "Boetebedrag ($)",
        "betterfines_options_speeding_min_delay": "Min. interval (sec)",
        "betterfines_options_speeding_over_limit": "Marge (km/u)",
        "betterfines_options_speeding_trigger_delay": "Triggervertraging (sec)",
        "betterfines_options_red_light_enabled": "Boetes voor rood licht",
        "betterfines_options_red_light_fine": "Boetebedrag ($)",
        "betterfines_options_red_light_min_delay": "Min. interval (sec)",
        "betterfines_options_red_light_min_speed": "Min. snelheid (km/u)",
        "betterfines_options_red_light_orange": "Boete voor oranje licht",
        "betterfines_warning_over_speed_limit": "waarschuwing! te hard rijden",
        "betterfines:sms_government_speeding_ticket": (
            "<b>Kennisgeving snelheidsovertreding</b><br><br>Geachte heer/mevrouw,<br><br>"
            "Hierbij informeren wij u dat uw voertuig van het type <b>{vehicleTypeName}</b> "
            "is geregistreerd met een snelheidsovertreding om {hour}:{minute}, dag {day}.<br><br>"
            "Er is automatisch een boete van <b>${amount}.00</b> afgeschreven van uw primaire bankrekening.<br><br>"
            "Met vriendelijke groet,<br>The New York City Department of Finance"
        ),
        "betterfines:sms_government_red_light_ticket": (
            "<b>Kennisgeving verkeersovertreding</b><br><br>Geachte heer/mevrouw,<br><br>"
            "Hierbij informeren wij u dat uw voertuig van het type <b>{vehicleTypeName}</b> "
            "is geregistreerd bij het passeren van rood licht om {hour}:{minute}, dag {day}.<br><br>"
            "Er is automatisch een boete van <b>${amount}.00</b> afgeschreven van uw primaire bankrekening.<br><br>"
            "Met vriendelijke groet,<br>The New York City Department of Finance"
        ),
    },
    "tr": {
        "betterfines_options_header": "Better Fines",
        "betterfines_options_speeding_enabled": "Hız cezaları",
        "betterfines_options_speeding_fine": "Ceza tutarı ($)",
        "betterfines_options_speeding_min_delay": "Min. gecikme (sn)",
        "betterfines_options_speeding_over_limit": "Tolerans (km/s)",
        "betterfines_options_speeding_trigger_delay": "Tetikleme gecikmesi (sn)",
        "betterfines_options_red_light_enabled": "Kırmızı ışık cezaları",
        "betterfines_options_red_light_fine": "Ceza tutarı ($)",
        "betterfines_options_red_light_min_delay": "Min. gecikme (sn)",
        "betterfines_options_red_light_min_speed": "Min. hız (km/s)",
        "betterfines_options_red_light_orange": "Turuncu ışık cezası",
        "betterfines_warning_over_speed_limit": "uyarı! hız sınırı aşıldı",
        "betterfines:sms_government_speeding_ticket": (
            "<b>Hız İhlali Bildirimi</b><br><br>Sayın yetkili,<br><br>"
            "<b>{vehicleTypeName}</b> tipindeki aracınızın {day}. gün {hour}:{minute} saatinde "
            "hız sınırını aştığı kaydedilmiştir.<br><br>"
            "Ana banka hesabınızdan otomatik olarak <b>${amount}.00</b> ceza tahsil edilmiştir.<br><br>"
            "Saygılarımızla,<br>New York Şehri Maliye Departmanı"
        ),
        "betterfines:sms_government_red_light_ticket": (
            "<b>Trafik İhlali Bildirimi</b><br><br>Sayın yetkili,<br><br>"
            "<b>{vehicleTypeName}</b> tipindeki aracınızın {day}. gün {hour}:{minute} saatinde "
            "kırmızı ışıkta geçtiği kaydedilmiştir.<br><br>"
            "Ana banka hesabınızdan otomatik olarak <b>${amount}.00</b> ceza tahsil edilmiştir.<br><br>"
            "Saygılarımızla,<br>New York Şehri Maliye Departmanı"
        ),
    },
    "ja": {
        "betterfines_options_header": "Better Fines",
        "betterfines_options_speeding_enabled": "速度超過の罰金",
        "betterfines_options_speeding_fine": "罰金額 ($)",
        "betterfines_options_speeding_min_delay": "最小間隔 (秒)",
        "betterfines_options_speeding_over_limit": "許容超過 (km/h)",
        "betterfines_options_speeding_trigger_delay": "発動遅延 (秒)",
        "betterfines_options_red_light_enabled": "赤信号の罰金",
        "betterfines_options_red_light_fine": "罰金額 ($)",
        "betterfines_options_red_light_min_delay": "最小間隔 (秒)",
        "betterfines_options_red_light_min_speed": "最低速度 (km/h)",
        "betterfines_options_red_light_orange": "黄信号の罰金",
        "betterfines_warning_over_speed_limit": "警告！速度超過",
        "betterfines:sms_government_speeding_ticket": (
            "<b>速度超過違反のお知らせ</b><br><br>拝啓<br><br>"
            "お客様の <b>{vehicleTypeName}</b> 型の車両が、{day}日目 {hour}:{minute} に速度超過で記録されました。<br><br>"
            "主要銀行口座から <b>${amount}.00</b> の罰金が自動的に引き落とされました。<br><br>"
            "敬具<br>ニューヨーク市財務局"
        ),
        "betterfines:sms_government_red_light_ticket": (
            "<b>交通違反のお知らせ</b><br><br>拝啓<br><br>"
            "お客様の <b>{vehicleTypeName}</b> 型の車両が、{day}日目 {hour}:{minute} に赤信号通過で記録されました。<br><br>"
            "主要銀行口座から <b>${amount}.00</b> の罰金が自動的に引き落とされました。<br><br>"
            "敬具<br>ニューヨーク市財務局"
        ),
    },
    "ko": {
        "betterfines_options_header": "Better Fines",
        "betterfines_options_speeding_enabled": "과속 벌금",
        "betterfines_options_speeding_fine": "벌금액 ($)",
        "betterfines_options_speeding_min_delay": "최소 간격 (초)",
        "betterfines_options_speeding_over_limit": "허용 초과 (km/h)",
        "betterfines_options_speeding_trigger_delay": "발동 지연 (초)",
        "betterfines_options_red_light_enabled": "신호 위반 벌금",
        "betterfines_options_red_light_fine": "벌금액 ($)",
        "betterfines_options_red_light_min_delay": "최소 간격 (초)",
        "betterfines_options_red_light_min_speed": "최소 속도 (km/h)",
        "betterfines_options_red_light_orange": "황색 신호 벌금",
        "betterfines_warning_over_speed_limit": "경고! 과속",
        "betterfines:sms_government_speeding_ticket": (
            "<b>과속 위반 통지</b><br><br>귀하께,<br><br>"
            "귀하의 <b>{vehicleTypeName}</b> 차량이 {day}일 {hour}:{minute}에 과속으로 기록되었습니다.<br><br>"
            "주 은행 계좌에서 <b>${amount}.00</b>의 벌금이 자동으로 출금되었습니다.<br><br>"
            "뉴욕시 재무부"
        ),
        "betterfines:sms_government_red_light_ticket": (
            "<b>교통 위반 통지</b><br><br>귀하께,<br><br>"
            "귀하의 <b>{vehicleTypeName}</b> 차량이 {day}일 {hour}:{minute}에 적색 신호 통과로 기록되었습니다.<br><br>"
            "주 은행 계좌에서 <b>${amount}.00</b>의 벌금이 자동으로 출금되었습니다.<br><br>"
            "뉴욕시 재무부"
        ),
    },
    "zh-CN": {
        "betterfines_options_header": "Better Fines",
        "betterfines_options_speeding_enabled": "超速罚款",
        "betterfines_options_speeding_fine": "罚款金额 ($)",
        "betterfines_options_speeding_min_delay": "最短间隔 (秒)",
        "betterfines_options_speeding_over_limit": "容许超速 (km/h)",
        "betterfines_options_speeding_trigger_delay": "触发延迟 (秒)",
        "betterfines_options_red_light_enabled": "闯红灯罚款",
        "betterfines_options_red_light_fine": "罚款金额 ($)",
        "betterfines_options_red_light_min_delay": "最短间隔 (秒)",
        "betterfines_options_red_light_min_speed": "最低速度 (km/h)",
        "betterfines_options_red_light_orange": "黄灯罚款",
        "betterfines_warning_over_speed_limit": "警告！超速",
        "betterfines:sms_government_speeding_ticket": (
            "<b>超速违章通知</b><br><br>尊敬的先生/女士：<br><br>"
            "您的 <b>{vehicleTypeName}</b> 型车辆于第 {day} 天 {hour}:{minute} 被记录超速。<br><br>"
            "已从您的主银行账户自动扣除 <b>${amount}.00</b> 罚款。<br><br>"
            "此致敬礼<br>纽约市财政局"
        ),
        "betterfines:sms_government_red_light_ticket": (
            "<b>交通违章通知</b><br><br>尊敬的先生/女士：<br><br>"
            "您的 <b>{vehicleTypeName}</b> 型车辆于第 {day} 天 {hour}:{minute} 被记录闯红灯。<br><br>"
            "已从您的主银行账户自动扣除 <b>${amount}.00</b> 罚款。<br><br>"
            "此致敬礼<br>纽约市财政局"
        ),
    },
    "zh-TW": {
        "betterfines_options_header": "Better Fines",
        "betterfines_options_speeding_enabled": "超速罰款",
        "betterfines_options_speeding_fine": "罰款金額 ($)",
        "betterfines_options_speeding_min_delay": "最短間隔 (秒)",
        "betterfines_options_speeding_over_limit": "容許超速 (km/h)",
        "betterfines_options_speeding_trigger_delay": "觸發延遲 (秒)",
        "betterfines_options_red_light_enabled": "闖紅燈罰款",
        "betterfines_options_red_light_fine": "罰款金額 ($)",
        "betterfines_options_red_light_min_delay": "最短間隔 (秒)",
        "betterfines_options_red_light_min_speed": "最低速度 (km/h)",
        "betterfines_options_red_light_orange": "黃燈罰款",
        "betterfines_warning_over_speed_limit": "警告！超速",
        "betterfines:sms_government_speeding_ticket": (
            "<b>超速違規通知</b><br><br>敬啟者：<br><br>"
            "您的 <b>{vehicleTypeName}</b> 型車輛於第 {day} 天 {hour}:{minute} 被記錄超速。<br><br>"
            "已從您的主要銀行帳戶自動扣除 <b>${amount}.00</b> 罰款。<br><br>"
            "敬上<br>紐約市財政局"
        ),
        "betterfines:sms_government_red_light_ticket": (
            "<b>交通違規通知</b><br><br>敬啟者：<br><br>"
            "您的 <b>{vehicleTypeName}</b> 型車輛於第 {day} 天 {hour}:{minute} 被記錄闖紅燈。<br><br>"
            "已從您的主要銀行帳戶自動扣除 <b>${amount}.00</b> 罰款。<br><br>"
            "敬上<br>紐約市財政局"
        ),
    },
    "cs": {
        "betterfines_options_header": "Better Fines",
        "betterfines_options_speeding_enabled": "Pokuty za překročení rychlosti",
        "betterfines_options_speeding_fine": "Výše pokuty ($)",
        "betterfines_options_speeding_min_delay": "Min. interval (s)",
        "betterfines_options_speeding_over_limit": "Tolerance (km/h)",
        "betterfines_options_speeding_trigger_delay": "Zpoždění spuštění (s)",
        "betterfines_options_red_light_enabled": "Pokuty za červenou",
        "betterfines_options_red_light_fine": "Výše pokuty ($)",
        "betterfines_options_red_light_min_delay": "Min. interval (s)",
        "betterfines_options_red_light_min_speed": "Min. rychlost (km/h)",
        "betterfines_options_red_light_orange": "Pokuta za oranžovou",
        "betterfines_warning_over_speed_limit": "varování! překročení rychlosti",
        "betterfines:sms_government_speeding_ticket": (
            "<b>Oznámení o překročení rychlosti</b><br><br>Vážený pane / paní,<br><br>"
            "informujeme vás, že vaše vozidlo typu <b>{vehicleTypeName}</b> "
            "bylo zaznamenáno s překročením rychlosti v {hour}:{minute}, den {day}.<br><br>"
            "Z vašeho hlavního bankovního účtu byla automaticky stržena pokuta ve výši <b>${amount}.00</b>.<br><br>"
            "S pozdravem<br>Finanční oddělení města New York"
        ),
        "betterfines:sms_government_red_light_ticket": (
            "<b>Oznámení o dopravním přestupku</b><br><br>Vážený pane / paní,<br><br>"
            "informujeme vás, že vaše vozidlo typu <b>{vehicleTypeName}</b> "
            "bylo zaznamenáno při průjezdu na červenou v {hour}:{minute}, den {day}.<br><br>"
            "Z vašeho hlavního bankovního účtu byla automaticky stržena pokuta ve výši <b>${amount}.00</b>.<br><br>"
            "S pozdravem<br>Finanční oddělení města New York"
        ),
    },
    "da": {
        "betterfines_options_header": "Better Fines",
        "betterfines_options_speeding_enabled": "Bøder for hastighedsoverskridelse",
        "betterfines_options_speeding_fine": "Bødebeløb ($)",
        "betterfines_options_speeding_min_delay": "Min. interval (sek)",
        "betterfines_options_speeding_over_limit": "Toleranse (km/t)",
        "betterfines_options_speeding_trigger_delay": "Udløsningsforsinkelse (sek)",
        "betterfines_options_red_light_enabled": "Bøder for rødt lys",
        "betterfines_options_red_light_fine": "Bødebeløb ($)",
        "betterfines_options_red_light_min_delay": "Min. interval (sek)",
        "betterfines_options_red_light_min_speed": "Min. hastighed (km/t)",
        "betterfines_options_red_light_orange": "Bøde for gult lys",
        "betterfines_warning_over_speed_limit": "advarsel! hastighedsoverskridelse",
        "betterfines:sms_government_speeding_ticket": (
            "<b>Meddelelse om hastighedsoverskridelse</b><br><br>Kære herrer / damer,<br><br>"
            "Vi skriver for at informere om, at dit køretøj af typen <b>{vehicleTypeName}</b> "
            "blev registreret med hastighedsoverskridelse kl. {hour}:{minute}, dag {day}.<br><br>"
            "Der er automatisk trukket en bøde på <b>${amount}.00</b> fra din primære bankkonto.<br><br>"
            "Med venlig hilsen<br>New York City Department of Finance"
        ),
        "betterfines:sms_government_red_light_ticket": (
            "<b>Meddelelse om trafikovertrædelse</b><br><br>Kære herrer / damer,<br><br>"
            "Vi skriver for at informere om, at dit køretøj af typen <b>{vehicleTypeName}</b> "
            "blev registreret ved kørsel over for rødt lys kl. {hour}:{minute}, dag {day}.<br><br>"
            "Der er automatisk trukket en bøde på <b>${amount}.00</b> fra din primære bankkonto.<br><br>"
            "Med venlig hilsen<br>New York City Department of Finance"
        ),
    },
    "fi": {
        "betterfines_options_header": "Better Fines",
        "betterfines_options_speeding_enabled": "Ylinopeussakot",
        "betterfines_options_speeding_fine": "Sakon määrä ($)",
        "betterfines_options_speeding_min_delay": "Min. väli (s)",
        "betterfines_options_speeding_over_limit": "Toleranssi (km/h)",
        "betterfines_options_speeding_trigger_delay": "Laukaisuviive (s)",
        "betterfines_options_red_light_enabled": "Punaisen valon sakot",
        "betterfines_options_red_light_fine": "Sakon määrä ($)",
        "betterfines_options_red_light_min_delay": "Min. väli (s)",
        "betterfines_options_red_light_min_speed": "Miniminopeus (km/h)",
        "betterfines_options_red_light_orange": "Keltaisen valon sakko",
        "betterfines_warning_over_speed_limit": "varoitus! ylinopeus",
        "betterfines:sms_government_speeding_ticket": (
            "<b>Ilmoitus ylinopeudesta</b><br><br>Hyvä herra / rouva,<br><br>"
            "ilmoitamme, että ajoneuvosi tyyppiä <b>{vehicleTypeName}</b> "
            "rekisteröitiin ylinopeutena klo {hour}:{minute}, päivä {day}.<br><br>"
            "Pääpankkitililtäsi on veloitettu automaattisesti sakko <b>${amount}.00</b>.<br><br>"
            "Ystävällisin terveisin<br>New Yorkin kaupungin rahoitusosasto"
        ),
        "betterfines:sms_government_red_light_ticket": (
            "<b>Ilmoitus liikennevirheestä</b><br><br>Hyvä herra / rouva,<br><br>"
            "ilmoitamme, että ajoneuvosi tyyppiä <b>{vehicleTypeName}</b> "
            "rekisteröitiin punaisen valon ylityksenä klo {hour}:{minute}, päivä {day}.<br><br>"
            "Pääpankkitililtäsi on veloitettu automaattisesti sakko <b>${amount}.00</b>.<br><br>"
            "Ystävällisin terveisin<br>New Yorkin kaupungin rahoitusosasto"
        ),
    },
    "el": {
        "betterfines_options_header": "Better Fines",
        "betterfines_options_speeding_enabled": "Πρόστιμα υπερβολικής ταχύτητας",
        "betterfines_options_speeding_fine": "Ποσό προστίμου ($)",
        "betterfines_options_speeding_min_delay": "Ελάχ. διάστημα (δευτ)",
        "betterfines_options_speeding_over_limit": "Ανοχή (km/h)",
        "betterfines_options_speeding_trigger_delay": "Καθυστέρηση ενεργοποίησης (δευτ)",
        "betterfines_options_red_light_enabled": "Πρόστιμα κόκκινου φαναριού",
        "betterfines_options_red_light_fine": "Ποσό προστίμου ($)",
        "betterfines_options_red_light_min_delay": "Ελάχ. διάστημα (δευτ)",
        "betterfines_options_red_light_min_speed": "Ελάχ. ταχύτητα (km/h)",
        "betterfines_options_red_light_orange": "Πρόστιμο πορτοκαλί φαναριού",
        "betterfines_warning_over_speed_limit": "προειδοποίηση! υπέρβαση ορίου",
        "betterfines:sms_government_speeding_ticket": (
            "<b>Ειδοποίηση υπερβολικής ταχύτητας</b><br><br>Αγαπητέ κύριε / κυρία,<br><br>"
            "σας ενημερώνουμε ότι το όχημά σας τύπου <b>{vehicleTypeName}</b> "
            "καταγράφηκε να υπερβαίνει το όριο ταχύτητας στις {hour}:{minute}, ημέρα {day}.<br><br>"
            "Από τον κύριο τραπεζικό σας λογαριασμό χρεώθηκε αυτόματα πρόστιμο <b>${amount}.00</b>.<br><br>"
            "Με εκτίμηση,<br>Τμήμα Οικονομικών της Πόλης της Νέας Υόρκης"
        ),
        "betterfines:sms_government_red_light_ticket": (
            "<b>Ειδοποίηση κυκλοφοριακής παράβασης</b><br><br>Αγαπητέ κύριε / κυρία,<br><br>"
            "σας ενημερώνουμε ότι το όχημά σας τύπου <b>{vehicleTypeName}</b> "
            "καταγράφηκε να περνά με κόκκινο φανάρι στις {hour}:{minute}, ημέρα {day}.<br><br>"
            "Από τον κύριο τραπεζικό σας λογαριασμό χρεώθηκε αυτόματα πρόστιμο <b>${amount}.00</b>.<br><br>"
            "Με εκτίμηση,<br>Τμήμα Οικονομικών της Πόλης της Νέας Υόρκης"
        ),
    },
    "hu": {
        "betterfines_options_header": "Better Fines",
        "betterfines_options_speeding_enabled": "Gyorshajtási bírságok",
        "betterfines_options_speeding_fine": "Bírság összege ($)",
        "betterfines_options_speeding_min_delay": "Min. időköz (mp)",
        "betterfines_options_speeding_over_limit": "Tűrés (km/h)",
        "betterfines_options_speeding_trigger_delay": "Késleltetés (mp)",
        "betterfines_options_red_light_enabled": "Piros lámpa bírságok",
        "betterfines_options_red_light_fine": "Bírság összege ($)",
        "betterfines_options_red_light_min_delay": "Min. időköz (mp)",
        "betterfines_options_red_light_min_speed": "Min. sebesség (km/h)",
        "betterfines_options_red_light_orange": "Sárga lámpa bírság",
        "betterfines_warning_over_speed_limit": "figyelem! gyorshajtás",
        "betterfines:sms_government_speeding_ticket": (
            "<b>Értesítés gyorshajtásról</b><br><br>Tisztelt Hölgyem / Uram!<br><br>"
            "Tájékoztatjuk, hogy <b>{vehicleTypeName}</b> típusú járművét "
            "a {day}. napon {hour}:{minute}-kor gyorshajtással rögzítették.<br><br>"
            "Elsődleges bankszámlájáról automatikusan levonásra került <b>${amount}.00</b> bírság.<br><br>"
            "Üdvözlettel,<br>New York Város Pénzügyi Osztálya"
        ),
        "betterfines:sms_government_red_light_ticket": (
            "<b>Értesítés közlekedési szabálysértésről</b><br><br>Tisztelt Hölgyem / Uram!<br><br>"
            "Tájékoztatjuk, hogy <b>{vehicleTypeName}</b> típusú járművét "
            "a {day}. napon {hour}:{minute}-kor piros lámpán való áthaladással rögzítették.<br><br>"
            "Elsődleges bankszámlájáról automatikusan levonásra került <b>${amount}.00</b> bírság.<br><br>"
            "Üdvözlettel,<br>New York Város Pénzügyi Osztálya"
        ),
    },
    "ro": {
        "betterfines_options_header": "Better Fines",
        "betterfines_options_speeding_enabled": "Amenzi pentru viteză",
        "betterfines_options_speeding_fine": "Valoarea amenzii ($)",
        "betterfines_options_speeding_min_delay": "Interval min. (sec)",
        "betterfines_options_speeding_over_limit": "Toleranță (km/h)",
        "betterfines_options_speeding_trigger_delay": "Întârziere declanșare (sec)",
        "betterfines_options_red_light_enabled": "Amenzi pentru roșu",
        "betterfines_options_red_light_fine": "Valoarea amenzii ($)",
        "betterfines_options_red_light_min_delay": "Interval min. (sec)",
        "betterfines_options_red_light_min_speed": "Viteză min. (km/h)",
        "betterfines_options_red_light_orange": "Amendă pentru galben",
        "betterfines_warning_over_speed_limit": "atenție! depășire viteză",
        "betterfines:sms_government_speeding_ticket": (
            "<b>Notificare depășire viteză</b><br><br>Stimate domn / Stimată doamnă,<br><br>"
            "vă informăm că vehiculul dvs. de tip <b>{vehicleTypeName}</b> "
            "a fost înregistrat cu depășirea vitezei la {hour}:{minute}, ziua {day}.<br><br>"
            "Din contul dvs. bancar principal a fost debitată automat o amendă de <b>${amount}.00</b>.<br><br>"
            "Cu stimă,<br>Departamentul de Finanțe al orașului New York"
        ),
        "betterfines:sms_government_red_light_ticket": (
            "<b>Notificare contravenție rutieră</b><br><br>Stimate domn / Stimată doamnă,<br><br>"
            "vă informăm că vehiculul dvs. de tip <b>{vehicleTypeName}</b> "
            "a fost înregistrat trecând pe roșu la {hour}:{minute}, ziua {day}.<br><br>"
            "Din contul dvs. bancar principal a fost debitată automat o amendă de <b>${amount}.00</b>.<br><br>"
            "Cu stimă,<br>Departamentul de Finanțe al orașului New York"
        ),
    },
    "uk": {
        "betterfines_options_header": "Better Fines",
        "betterfines_options_speeding_enabled": "Штрафи за перевищення швидкості",
        "betterfines_options_speeding_fine": "Сума штрафу ($)",
        "betterfines_options_speeding_min_delay": "Мін. інтервал (сек)",
        "betterfines_options_speeding_over_limit": "Допуск (км/год)",
        "betterfines_options_speeding_trigger_delay": "Затримка спрацювання (сек)",
        "betterfines_options_red_light_enabled": "Штрафи за червоне світло",
        "betterfines_options_red_light_fine": "Сума штрафу ($)",
        "betterfines_options_red_light_min_delay": "Мін. інтервал (сек)",
        "betterfines_options_red_light_min_speed": "Мін. швидкість (км/год)",
        "betterfines_options_red_light_orange": "Штраф за жовте світло",
        "betterfines_warning_over_speed_limit": "увага! перевищення швидкості",
        "betterfines:sms_government_speeding_ticket": (
            "<b>Повідомлення про перевищення швидкості</b><br><br>Шановний пане / пані,<br><br>"
            "повідомляємо, що ваш автомобіль типу <b>{vehicleTypeName}</b> "
            "був зафіксований з перевищенням швидкості о {hour}:{minute}, день {day}.<br><br>"
            "З вашого основного банківського рахунку автоматично списано штраф у розмірі <b>${amount}.00</b>.<br><br>"
            "З повагою,<br>Департамент фінансів міста Нью-Йорк"
        ),
        "betterfines:sms_government_red_light_ticket": (
            "<b>Повідомлення про порушення ПДР</b><br><br>Шановний пане / пані,<br><br>"
            "повідомляємо, що ваш автомобіль типу <b>{vehicleTypeName}</b> "
            "був зафіксований при проїзді на червоне світло о {hour}:{minute}, день {day}.<br><br>"
            "З вашого основного банківського рахунку автоматично списано штраф у розмірі <b>${amount}.00</b>.<br><br>"
            "З повагою,<br>Департамент фінансів міста Нью-Йорк"
        ),
    },
    "lt": {
        "betterfines_options_header": "Better Fines",
        "betterfines_options_speeding_enabled": "Baudos už greičio viršijimą",
        "betterfines_options_speeding_fine": "Baudos suma ($)",
        "betterfines_options_speeding_min_delay": "Min. intervalas (sek)",
        "betterfines_options_speeding_over_limit": "Tolerancija (km/h)",
        "betterfines_options_speeding_trigger_delay": "Suveikimo delsa (sek)",
        "betterfines_options_red_light_enabled": "Baudos už raudoną šviesą",
        "betterfines_options_red_light_fine": "Baudos suma ($)",
        "betterfines_options_red_light_min_delay": "Min. intervalas (sek)",
        "betterfines_options_red_light_min_speed": "Min. greitis (km/h)",
        "betterfines_options_red_light_orange": "Bauda už geltoną šviesą",
        "betterfines_warning_over_speed_limit": "įspėjimas! greičio viršijimas",
        "betterfines:sms_government_speeding_ticket": (
            "<b>Pranešimas apie greičio viršijimą</b><br><br>Gerbiamas(-a) ponas(-ė),<br><br>"
            "informuojame, kad jūsų <b>{vehicleTypeName}</b> tipo transporto priemonė "
            "buvo užfiksuota viršijant greitį {hour}:{minute}, {day} dieną.<br><br>"
            "Iš jūsų pagrindinės banko sąskaitos automatiškai nuskaičiuota <b>${amount}.00</b> bauda.<br><br>"
            "Pagarbiai,<br Niujorko miesto finansų departamentas"
        ),
        "betterfines:sms_government_red_light_ticket": (
            "<b>Pranešimas apie eismo taisyklių pažeidimą</b><br><br>Gerbiamas(-a) ponas(-ė),<br><br>"
            "informuojame, kad jūsų <b>{vehicleTypeName}</b> tipo transporto priemonė "
            "buvo užfiksuota važiuojant per raudoną šviesą {hour}:{minute}, {day} dieną.<br><br>"
            "Iš jūsų pagrindinės banko sąskaitos automatiškai nuskaičiuota <b>${amount}.00</b> bauda.<br><br>"
            "Pagarbiai,<br>Niujorko miesto finansų departamentas"
        ),
    },
}

WRONG_WAY: dict[str, dict[str, str]] = {
    "en": {
        "betterfines_options_wrong_way_enabled": "Wrong-way fines",
        "betterfines_options_wrong_way_fine": "Fine value ($)",
        "betterfines_options_wrong_way_min_delay": "Min delay (sec)",
        "betterfines_options_wrong_way_min_speed": "Min speed (km/h)",
        "betterfines_options_wrong_way_trigger_delay": "Trigger delay (sec)",
        "betterfines_warning_wrong_way": "warning! wrong way",
        "betterfines:sms_government_wrong_way_ticket": (
            "<b>Notice of Wrong-Way Driving Violation</b><br><br>Dear Sir or Madam:<br><br>"
            "We are writing to inform you that your motor vehicle of type <b>{vehicleTypeName}</b> "
            "was recorded driving against traffic at {hour}:{minute}, Day {day}.<br><br>"
            "You have been automatically charged a fine of <b>${amount}.00</b> from your primary bank account.<br><br>"
            "Sincerely,<br>The New York City Department of Finance"
        ),
    },
    "fr": {
        "betterfines_options_wrong_way_enabled": "Amendes sens interdit",
        "betterfines_options_wrong_way_fine": "Montant de l'amende ($)",
        "betterfines_options_wrong_way_min_delay": "Délai minimum (sec)",
        "betterfines_options_wrong_way_min_speed": "Vitesse minimum (km/h)",
        "betterfines_options_wrong_way_trigger_delay": "Délai avant amende (sec)",
        "betterfines_warning_wrong_way": "attention ! sens interdit",
        "betterfines:sms_government_wrong_way_ticket": (
            "<b>Avis d'infraction pour circulation en sens interdit</b><br><br>Madame, Monsieur,<br><br>"
            "Nous vous informons que votre véhicule de type <b>{vehicleTypeName}</b> "
            "a été enregistré circulant en sens interdit à {hour}:{minute}, jour {day}.<br><br>"
            "Un prélèvement automatique de <b>{amount} $</b> a été effectué sur votre compte bancaire principal.<br><br>"
            "Cordialement,<br>Le Département des Finances de la Ville de New York"
        ),
    },
}

VALUE_UNITS: dict[str, dict[str, str]] = {
    "en": {
        "betterfines_options_value_dollars": "${value}",
        "betterfines_options_value_seconds": "{value} sec",
        "betterfines_options_value_kmh": "{value} km/h",
        "betterfines_options_value_days": "{value} days",
        "betterfines_options_value_percent": "{value}%",
        "betterfines_options_value_count": "{value}",
    },
    "fr": {
        "betterfines_options_value_dollars": "{value} $",
        "betterfines_options_value_seconds": "{value} sec",
        "betterfines_options_value_kmh": "{value} km/h",
        "betterfines_options_value_days": "{value} j",
        "betterfines_options_value_percent": "{value} %",
        "betterfines_options_value_count": "{value}",
    },
}

RECIDIVISM: dict[str, dict[str, str]] = {
    "en": {
        "betterfines_options_recidivism_header": "Recidivism",
        "betterfines_options_recidivism_enabled": "Repeat-offense surcharges",
        "betterfines_options_fine_lifetime_days": "Fine lifetime (days)",
        "betterfines_options_recidivism_tier1_count": "Tier 1 from (fines)",
        "betterfines_options_recidivism_tier1_percent": "Tier 1 surcharge (%)",
        "betterfines_options_recidivism_tier2_count": "Tier 2 from (fines)",
        "betterfines_options_recidivism_tier2_percent": "Tier 2 surcharge (%)",
        "betterfines_options_license_revoke_enabled": "License suspension",
        "betterfines_options_license_revoke_count": "Suspend at (fines)",
        "betterfines_panel_title": "ACTIVE FINES",
        "betterfines_panel_title_count": "ACTIVE FINES ({count})",
        "betterfines_panel_fine_line": "{type} ${amount} — expires in {days}d",
        "betterfines_panel_total": "Total: ${total}",
        "betterfines_panel_surcharge": "Surcharge: +{percent}%",
        "betterfines_panel_license_suspended": "License suspended — {days}d remaining",
        "betterfines_violation_speeding": "Speeding",
        "betterfines_violation_red_light": "Red light",
        "betterfines_violation_wrong_way": "Wrong way",
        "betterfines_warning_license_suspended": "warning! your license is suspended and you cannot drive",
    },
    "fr": {
        "betterfines_options_recidivism_header": "Récidive",
        "betterfines_options_recidivism_enabled": "Majorations récidive",
        "betterfines_options_fine_lifetime_days": "Durée de vie amende (jours)",
        "betterfines_options_recidivism_tier1_count": "Palier 1 à partir de (amendes)",
        "betterfines_options_recidivism_tier1_percent": "Majoration palier 1 (%)",
        "betterfines_options_recidivism_tier2_count": "Palier 2 à partir de (amendes)",
        "betterfines_options_recidivism_tier2_percent": "Majoration palier 2 (%)",
        "betterfines_options_license_revoke_enabled": "Retrait du permis",
        "betterfines_options_license_revoke_count": "Retirer à (amendes)",
        "betterfines_panel_title": "AMENDES EN COURS",
        "betterfines_panel_title_count": "AMENDES EN COURS ({count})",
        "betterfines_panel_fine_line": "{type} {amount} $ — expire dans {days} j",
        "betterfines_panel_total": "Total : {total} $",
        "betterfines_panel_surcharge": "Majoration : +{percent} %",
        "betterfines_panel_license_suspended": "Permis retiré — {days} j restants",
        "betterfines_violation_speeding": "Excès de vitesse",
        "betterfines_violation_red_light": "Feu rouge",
        "betterfines_violation_wrong_way": "Sens interdit",
        "betterfines_warning_license_suspended": "attention ! permis retiré, vous ne pouvez pas conduire",
    },
}

for _locale, _strings in LOCALES.items():
    _strings.update(WRONG_WAY.get(_locale, WRONG_WAY["en"]))
    _strings.update(VALUE_UNITS.get(_locale, VALUE_UNITS["en"]))
    _strings.update(RECIDIVISM.get(_locale, RECIDIVISM["en"]))


def main() -> None:
    OUT.mkdir(parents=True, exist_ok=True)
    en_keys = list(LOCALES["en"].keys())

    for locale, strings in LOCALES.items():
        if set(strings.keys()) != set(en_keys):
            missing = set(en_keys) - set(strings)
            extra = set(strings) - set(en_keys)
            raise SystemExit(f"{locale}: key mismatch missing={missing} extra={extra}")

        path = OUT / f"{locale}.json"
        with path.open("w", encoding="utf-8", newline="\n") as f:
            json.dump(strings, f, ensure_ascii=False, indent=2)
            f.write("\n")
        print(f"wrote {path.name}")

    print(f"done: {len(LOCALES)} locales, {len(en_keys)} keys each")


if __name__ == "__main__":
    main()
