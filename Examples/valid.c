int main() {
    int x = 10;
    float y = 2.5e1;
    int h = 0x2A;
    char c = 'a';
    string text = "hello";

    if (x < y) {
        x = x + h;
    }

    while (x > 0) {
        x = x - 1;
    }

    return x;
}
